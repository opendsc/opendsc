// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.FileSystem.Directory;

[DscResource("OpenDsc.FileSystem/Directory", "0.1.0", Description = "Manage directories", Tags = ["directory", "filesystem"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(SecurityException), Description = "Access denied")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[ExitCode(5, Exception = typeof(IOException), Description = "IO error")]
[ExitCode(6, Exception = typeof(UnauthorizedAccessException), Description = "Access denied")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>, ITestable<Schema>
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

        var fullPath = Path.GetFullPath(instance.Path);
        return System.IO.Directory.Exists(fullPath)
            ? new Schema { Path = instance.Path }
            : new Schema { Path = instance.Path, Exist = false };
    }

    public TestResult<Schema> Test(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var fullPath = Path.GetFullPath(instance.Path);
        bool inDesiredState;
        if (string.IsNullOrEmpty(instance.SourcePath))
        {
            inDesiredState = System.IO.Directory.Exists(fullPath) == (instance.Exist != false);
        }
        else
        {
            var sourceFullPath = Path.GetFullPath(instance.SourcePath);
            inDesiredState = System.IO.Directory.Exists(fullPath) && DirectoriesMatch(fullPath, sourceFullPath);
        }
        var currentState = Get(instance);
        currentState.InDesiredState = inDesiredState;
        return new TestResult<Schema>(currentState);
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var fullPath = Path.GetFullPath(instance.Path);

        if (!System.IO.Directory.Exists(fullPath))
        {
            System.IO.Directory.CreateDirectory(fullPath);
            if (!string.IsNullOrEmpty(instance.SourcePath))
            {
                var sourceFullPath = Path.GetFullPath(instance.SourcePath);
                if (!System.IO.Directory.Exists(sourceFullPath))
                {
                    throw new IOException("Source directory does not exist.");
                }
                CopyDirectoryRecursively(sourceFullPath, fullPath);
            }
        }

        return null;
    }

    public void Delete(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var fullPath = Path.GetFullPath(instance.Path);
        if (System.IO.Directory.Exists(fullPath))
        {
            System.IO.Directory.Delete(fullPath);
        }
    }

    private static void CopyDirectoryRecursively(string sourceDir, string targetDir)
    {
        foreach (var dir in System.IO.Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, dir);
            var targetSubDir = Path.Combine(targetDir, relativePath);
            System.IO.Directory.CreateDirectory(targetSubDir);
        }

        foreach (var file in System.IO.Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var targetFile = Path.Combine(targetDir, relativePath);
            System.IO.File.Copy(file, targetFile, overwrite: true);
        }
    }

    private static bool DirectoriesMatch(string targetDir, string sourceDir)
    {
        if (!System.IO.Directory.Exists(targetDir) || !System.IO.Directory.Exists(sourceDir))
        {
            return false;
        }

        static string GetRelativePath(string fullPath, string baseDir) => Path.GetRelativePath(baseDir, fullPath);

        var sourceDirs = System.IO.Directory.EnumerateDirectories(sourceDir, "*", SearchOption.AllDirectories)
            .Select(d => GetRelativePath(d, sourceDir))
            .ToHashSet(StringComparer.Ordinal);

        var targetDirs = System.IO.Directory.EnumerateDirectories(targetDir, "*", SearchOption.AllDirectories)
            .Select(d => GetRelativePath(d, targetDir))
            .ToHashSet(StringComparer.Ordinal);

        if (!sourceDirs.IsSubsetOf(targetDirs))
        {
            return false;
        }

        var sourceFiles = System.IO.Directory.EnumerateFiles(sourceDir, "*", SearchOption.AllDirectories)
            .Select(f => GetRelativePath(f, sourceDir))
            .ToDictionary(f => f, f => Path.Combine(sourceDir, f), StringComparer.Ordinal);

        var targetFiles = System.IO.Directory.EnumerateFiles(targetDir, "*", SearchOption.AllDirectories)
            .Select(f => GetRelativePath(f, targetDir))
            .ToDictionary(f => f, f => Path.Combine(targetDir, f), StringComparer.Ordinal);

        if (!sourceFiles.Keys.ToHashSet(StringComparer.Ordinal).IsSubsetOf(targetFiles.Keys.ToHashSet(StringComparer.Ordinal)))
        {
            return false;
        }

        foreach (var kvp in sourceFiles)
        {
            var sourceFile = kvp.Value;
            var targetFile = targetFiles[kvp.Key];
            var sourceInfo = new FileInfo(sourceFile);
            var targetInfo = new FileInfo(targetFile);

            if (sourceInfo.Length != targetInfo.Length)
            {
                return false;
            }

            if (ComputeSha256(sourceFile) != ComputeSha256(targetFile))
            {
                return false;
            }
        }

        return true;
    }

    private static string ComputeSha256(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = System.IO.File.OpenRead(filePath);
        using var bufferedStream = new BufferedStream(stream, 8192);
        var hash = sha256.ComputeHash(bufferedStream);
        return Convert.ToHexStringLower(hash);
    }
}
