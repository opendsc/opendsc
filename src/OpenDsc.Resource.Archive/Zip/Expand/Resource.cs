// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Archive.Zip.Expand;

[DscResource("OpenDsc.Archive.Zip/Expand", "0.1.0", Description = "Extract ZIP archives", Tags = ["archive", "zip", "extraction"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(FileNotFoundException), Description = "Archive not found")]
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

    public Schema Get(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        return new Schema
        {
            ArchivePath = instance.ArchivePath,
            DestinationPath = instance.DestinationPath
        };
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var archivePath = Path.GetFullPath(instance.ArchivePath);
        var destinationPath = Path.GetFullPath(instance.DestinationPath);

        if (!File.Exists(archivePath))
        {
            throw new FileNotFoundException($"Archive not found: {archivePath}");
        }

        if (!Directory.Exists(destinationPath))
        {
            Directory.CreateDirectory(destinationPath);
        }

        ZipFile.ExtractToDirectory(archivePath, destinationPath, overwriteFiles: true);

        return null;
    }

    public TestResult<Schema> Test(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var archivePath = Path.GetFullPath(instance.ArchivePath);
        var destinationPath = Path.GetFullPath(instance.DestinationPath);

        if (!File.Exists(archivePath))
        {
            return new TestResult<Schema>(new Schema
            {
                ArchivePath = instance.ArchivePath,
                DestinationPath = instance.DestinationPath,
                InDesiredState = false
            });
        }

        if (!Directory.Exists(destinationPath))
        {
            return new TestResult<Schema>(new Schema
            {
                ArchivePath = instance.ArchivePath,
                DestinationPath = instance.DestinationPath,
                InDesiredState = false
            });
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

        foreach (var entry in archiveEntries)
        {
            var filePath = Path.Combine(destinationPath, entry.Key);

            if (!File.Exists(filePath))
            {
                return new TestResult<Schema>(new Schema
                {
                    ArchivePath = instance.ArchivePath,
                    DestinationPath = instance.DestinationPath,
                    InDesiredState = false
                });
            }

            var fileCrc32 = ZipHelper.ComputeCrc32(filePath);
            if (fileCrc32 != entry.Value)
            {
                return new TestResult<Schema>(new Schema
                {
                    ArchivePath = instance.ArchivePath,
                    DestinationPath = instance.DestinationPath,
                    InDesiredState = false
                });
            }
        }

        return new TestResult<Schema>(new Schema
        {
            ArchivePath = instance.ArchivePath,
            DestinationPath = instance.DestinationPath,
            InDesiredState = true
        });
    }
}
