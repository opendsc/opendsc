// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Archive.Zip.Compress;

[DscResource("OpenDsc.Archive.Zip/Compress", "0.1.0", Description = "Create ZIP archives", Tags = ["archive", "zip", "compression"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(FileNotFoundException), Description = "Source path not found")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[ExitCode(5, Exception = typeof(IOException), Description = "IO error")]
[ExitCode(6, Exception = typeof(InvalidDataException), Description = "Invalid or corrupt archive")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, ITestable<Schema>
{
    public override string GetSchema()
    {
        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase
        };

        var builder = new JsonSchemaBuilder().FromType<Schema>(config);
        builder.Schema("https://json-schema.org/draft/2020-12/schema");
        var schema = builder.Build();

        return JsonSerializer.Serialize(schema);
    }

    public Schema Get(Schema instance)
    {
        return new Schema
        {
            ArchivePath = instance.ArchivePath,
            SourcePath = instance.SourcePath,
            CompressionLevel = instance.CompressionLevel
        };
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        var archivePath = Path.GetFullPath(instance.ArchivePath);
        var sourcePath = Path.GetFullPath(instance.SourcePath);

        if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Source path not found: {sourcePath}");
        }

        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }

        var archiveDir = Path.GetDirectoryName(archivePath);
        if (!string.IsNullOrEmpty(archiveDir) && !Directory.Exists(archiveDir))
        {
            Directory.CreateDirectory(archiveDir);
        }

        var compressionLevel = instance.CompressionLevel ?? CompressionLevel.Optimal;

        if (Directory.Exists(sourcePath))
        {
            ZipFile.CreateFromDirectory(sourcePath, archivePath, compressionLevel, includeBaseDirectory: false);
        }
        else
        {
            using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
            var fileName = Path.GetFileName(sourcePath);
            archive.CreateEntryFromFile(sourcePath, fileName, compressionLevel);
        }

        return null;
    }

    public TestResult<Schema> Test(Schema instance)
    {
        var archivePath = Path.GetFullPath(instance.ArchivePath);
        var sourcePath = Path.GetFullPath(instance.SourcePath);

        if (!File.Exists(archivePath))
        {
            return new TestResult<Schema>(new Schema
            {
                ArchivePath = instance.ArchivePath,
                SourcePath = instance.SourcePath,
                InDesiredState = false
            });
        }

        if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
        {
            return new TestResult<Schema>(new Schema
            {
                ArchivePath = instance.ArchivePath,
                SourcePath = instance.SourcePath,
                InDesiredState = false
            });
        }

        var sourceFiles = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

        if (Directory.Exists(sourcePath))
        {
            foreach (var file in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourcePath, file);
                var crc32 = ZipHelper.ComputeCrc32(file);
                sourceFiles[relativePath.Replace('\\', '/')] = crc32;
            }
        }
        else
        {
            var fileName = Path.GetFileName(sourcePath);
            var crc32 = ZipHelper.ComputeCrc32(sourcePath);
            sourceFiles[fileName] = crc32;
        }

        using var archive = ZipFile.OpenRead(archivePath);
        var archiveEntries = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in archive.Entries)
        {
            if (!string.IsNullOrEmpty(entry.Name))
            {
                archiveEntries[entry.FullName] = entry.Crc32;
            }
        }

        var inDesiredState = sourceFiles.Count == archiveEntries.Count &&
                           sourceFiles.All(sf => archiveEntries.TryGetValue(sf.Key, out var crc) && crc == sf.Value);

        return new TestResult<Schema>(new Schema
        {
            ArchivePath = instance.ArchivePath,
            SourcePath = instance.SourcePath,
            CompressionLevel = instance.CompressionLevel,
            InDesiredState = inDesiredState
        });
    }
}
