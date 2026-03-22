// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System;
using System.IO;
using System.IO.Compression;

using AwesomeAssertions;

using Xunit;

using ExpandResource = OpenDsc.Resource.Archive.Zip.Expand.Resource;
using ExpandSchema = OpenDsc.Resource.Archive.Zip.Expand.Schema;

namespace OpenDsc.Resource.Archive.Tests.Zip;

[Trait("Category", "Integration")]
public sealed class ExpandTests : IDisposable
{
    private readonly ExpandResource _resource = new(OpenDsc.Resource.Archive.SourceGenerationContext.Default);
    private readonly string _workPath = Path.Combine(Path.GetTempPath(), "OpenDsc.Resource.Archive.Tests", Guid.NewGuid().ToString("N"));

    public ExpandTests()
    {
        Directory.CreateDirectory(_workPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workPath))
        {
            Directory.Delete(_workPath, recursive: true);
        }
    }

    private string GetTempPath(string fileName) => Path.Combine(_workPath, fileName);

    private static void CreateZip(string sourcePath, string archivePath)
    {
        if (Directory.Exists(sourcePath))
        {
            Directory.Delete(sourcePath, recursive: true);
        }

        Directory.CreateDirectory(sourcePath);
        ZipFile.CreateFromDirectory(sourcePath, archivePath);
    }

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = System.Text.Json.JsonDocument.Parse(schemaJson);

        doc.RootElement.GetProperty("$schema").GetString()
            .Should().Be("https://json-schema.org/draft/2020-12/schema");
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectTypeAndVersion()
    {
        var attr = typeof(ExpandResource).GetCustomAttributes(typeof(DscResourceAttribute), false)
            .OfType<DscResourceAttribute>().SingleOrDefault();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Archive.Zip/Expand");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_ReturnsSameArchiveInfo()
    {
        var archivePath = GetTempPath("test.zip");
        var destinationPath = GetTempPath("destination");

        var actual = _resource.Get(new ExpandSchema
        {
            ArchivePath = archivePath,
            DestinationPath = destinationPath
        });

        actual.ArchivePath.Should().Be(archivePath);
        actual.DestinationPath.Should().Be(destinationPath);
    }

    [Fact]
    public void Set_ExtractsArchiveToDestination()
    {
        var archivePath = GetTempPath("extract.zip");
        var sourcePath = GetTempPath("source");
        var destinationPath = GetTempPath("destination");

        Directory.CreateDirectory(sourcePath);
        File.WriteAllText(Path.Combine(sourcePath, "file1.txt"), "Test content 1");
        File.WriteAllText(Path.Combine(sourcePath, "file2.txt"), "Test content 2");

        ZipFile.CreateFromDirectory(sourcePath, archivePath);

        _resource.Set(new ExpandSchema
        {
            ArchivePath = archivePath,
            DestinationPath = destinationPath
        });

        File.Exists(Path.Combine(destinationPath, "file1.txt")).Should().BeTrue();
        File.Exists(Path.Combine(destinationPath, "file2.txt")).Should().BeTrue();
        File.ReadAllText(Path.Combine(destinationPath, "file1.txt")).Should().Be("Test content 1");
    }

    [Fact]
    public void Set_CreatesDestinationDirectoryIfMissing()
    {
        var archivePath = GetTempPath("dest.zip");
        var sourcePath = GetTempPath("source-dir");
        var destinationPath = GetTempPath("dest");

        Directory.CreateDirectory(sourcePath);
        File.WriteAllText(Path.Combine(sourcePath, "file.txt"), "Test content");

        ZipFile.CreateFromDirectory(sourcePath, archivePath);

        Directory.Exists(destinationPath).Should().BeFalse();

        _resource.Set(new ExpandSchema
        {
            ArchivePath = archivePath,
            DestinationPath = destinationPath
        });

        Directory.Exists(destinationPath).Should().BeTrue();
    }

    [Fact]
    public void Set_OverwritesExistingFiles()
    {
        var archivePath = GetTempPath("overwrite.zip");
        var sourcePath = GetTempPath("source-overwrite");
        var destinationPath = GetTempPath("dest-overwrite");

        Directory.CreateDirectory(sourcePath);
        File.WriteAllText(Path.Combine(sourcePath, "file1.txt"), "New content");

        ZipFile.CreateFromDirectory(sourcePath, archivePath);

        Directory.CreateDirectory(destinationPath);
        File.WriteAllText(Path.Combine(destinationPath, "file1.txt"), "Old content");

        _resource.Set(new ExpandSchema
        {
            ArchivePath = archivePath,
            DestinationPath = destinationPath
        });

        File.ReadAllText(Path.Combine(destinationPath, "file1.txt")).Should().Be("New content");
    }

    [Fact]
    public void Set_PreservesExistingFilesNotInArchive()
    {
        var archivePath = GetTempPath("additive.zip");
        var sourcePath = GetTempPath("source-additive");
        var destinationPath = GetTempPath("dest-additive");

        Directory.CreateDirectory(sourcePath);
        File.WriteAllText(Path.Combine(sourcePath, "file1.txt"), "Archive content");

        ZipFile.CreateFromDirectory(sourcePath, archivePath);

        Directory.CreateDirectory(destinationPath);
        File.WriteAllText(Path.Combine(destinationPath, "existing.txt"), "Existing content");

        _resource.Set(new ExpandSchema
        {
            ArchivePath = archivePath,
            DestinationPath = destinationPath
        });

        File.Exists(Path.Combine(destinationPath, "existing.txt")).Should().BeTrue();
        File.ReadAllText(Path.Combine(destinationPath, "existing.txt")).Should().Be("Existing content");
    }

    [Fact]
    public void Set_ThrowsWhenArchiveDoesNotExist()
    {
        var archivePath = GetTempPath("missing.zip");
        var destinationPath = GetTempPath("dest-missing");

        Assert.Throws<FileNotFoundException>(() => _resource.Set(new ExpandSchema
        {
            ArchivePath = archivePath,
            DestinationPath = destinationPath
        }));
    }

    [Fact]
    public void Set_HandlesEmptyArchive()
    {
        var archivePath = GetTempPath("empty.zip");
        var destinationPath = GetTempPath("dest-empty");

        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
        }

        _resource.Set(new ExpandSchema
        {
            ArchivePath = archivePath,
            DestinationPath = destinationPath
        });

        Directory.Exists(destinationPath).Should().BeTrue();
        Directory.GetFiles(destinationPath, "*", SearchOption.AllDirectories).Length.Should().Be(0);
    }

    [Fact]
    public void Set_HandlesNestedDirectories()
    {
        var archivePath = GetTempPath("nested.zip");
        var sourcePath = GetTempPath("source-nested");
        var destinationPath = GetTempPath("dest-nested");

        var nestedSource = Path.Combine(sourcePath, "subdir");
        Directory.CreateDirectory(nestedSource);
        File.WriteAllText(Path.Combine(nestedSource, "nested.txt"), "Nested content");

        ZipFile.CreateFromDirectory(sourcePath, archivePath);

        _resource.Set(new ExpandSchema
        {
            ArchivePath = archivePath,
            DestinationPath = destinationPath
        });

        var extracted = Path.Combine(destinationPath, "subdir", "nested.txt");
        File.Exists(extracted).Should().BeTrue();
        File.ReadAllText(extracted).Should().Be("Nested content");
    }

    [Fact]
    public void Set_HandlesEmptyFilesInArchive()
    {
        var archivePath = GetTempPath("emptyfile.zip");
        var sourcePath = GetTempPath("source-emptyfile");
        var destinationPath = GetTempPath("dest-emptyfile");

        Directory.CreateDirectory(sourcePath);
        var emptyFile = Path.Combine(sourcePath, "empty.txt");
        File.WriteAllText(emptyFile, string.Empty);

        ZipFile.CreateFromDirectory(sourcePath, archivePath);

        _resource.Set(new ExpandSchema
        {
            ArchivePath = archivePath,
            DestinationPath = destinationPath
        });

        var extracted = Path.Combine(destinationPath, "empty.txt");
        File.Exists(extracted).Should().BeTrue();
        new FileInfo(extracted).Length.Should().Be(0);
    }

    [Fact]
    public void Test_ReturnsFalse_WhenArchiveDoesNotExist()
    {
        var archivePath = GetTempPath("missing-test.zip");
        var destinationPath = GetTempPath("dest-test-missing");

        var testResult = _resource.Test(new ExpandSchema { ArchivePath = archivePath, DestinationPath = destinationPath });

        testResult.ActualState.InDesiredState.Should().BeFalse();
    }

    [Fact]
    public void Test_ReturnsFalse_WhenDestinationDoesNotExist()
    {
        var archivePath = GetTempPath("dest-missing.zip");
        var sourcePath = GetTempPath("source-test");
        var destinationPath = GetTempPath("dest-test");

        Directory.CreateDirectory(sourcePath);
        File.WriteAllText(Path.Combine(sourcePath, "file.txt"), "Test content");
        ZipFile.CreateFromDirectory(sourcePath, archivePath);

        var testResult = _resource.Test(new ExpandSchema { ArchivePath = archivePath, DestinationPath = destinationPath });

        testResult.ActualState.InDesiredState.Should().BeFalse();
    }

    [Fact]
    public void Test_ReturnsTrue_WhenDestinationMatchesArchive()
    {
        var archivePath = GetTempPath("test-match.zip");
        var sourcePath = GetTempPath("source-test-match");
        var destinationPath = GetTempPath("dest-test-match");

        Directory.CreateDirectory(sourcePath);
        File.WriteAllText(Path.Combine(sourcePath, "file.txt"), "Test content");
        ZipFile.CreateFromDirectory(sourcePath, archivePath);

        _resource.Set(new ExpandSchema { ArchivePath = archivePath, DestinationPath = destinationPath });

        var testResult = _resource.Test(new ExpandSchema { ArchivePath = archivePath, DestinationPath = destinationPath });

        testResult.ActualState.InDesiredState.Should().BeTrue();
    }

    [Fact]
    public void Test_ReturnsFalse_WhenFileMissingFromDestination()
    {
        var archivePath = GetTempPath("missing-file.zip");
        var sourcePath = GetTempPath("source-missing-file");
        var destinationPath = GetTempPath("dest-missing-file");

        Directory.CreateDirectory(sourcePath);
        File.WriteAllText(Path.Combine(sourcePath, "file.txt"), "Test content");
        ZipFile.CreateFromDirectory(sourcePath, archivePath);

        Directory.CreateDirectory(destinationPath);

        var testResult = _resource.Test(new ExpandSchema { ArchivePath = archivePath, DestinationPath = destinationPath });

        testResult.ActualState.InDesiredState.Should().BeFalse();
    }

    [Fact]
    public void Test_ReturnsFalse_WhenFileContentDiffers()
    {
        var archivePath = GetTempPath("content-diff.zip");
        var sourcePath = GetTempPath("source-content-diff");
        var destinationPath = GetTempPath("dest-content-diff");

        Directory.CreateDirectory(sourcePath);
        File.WriteAllText(Path.Combine(sourcePath, "file.txt"), "Original content");
        ZipFile.CreateFromDirectory(sourcePath, archivePath);

        _resource.Set(new ExpandSchema { ArchivePath = archivePath, DestinationPath = destinationPath });
        File.WriteAllText(Path.Combine(destinationPath, "file.txt"), "Modified content");

        var testResult = _resource.Test(new ExpandSchema { ArchivePath = archivePath, DestinationPath = destinationPath });

        testResult.ActualState.InDesiredState.Should().BeFalse();
    }

    [Fact]
    public void Test_ReturnsTrue_WhenExtraFilesExistInDestination()
    {
        var archivePath = GetTempPath("extra-files.zip");
        var sourcePath = GetTempPath("source-extra-files");
        var destinationPath = GetTempPath("dest-extra-files");

        Directory.CreateDirectory(sourcePath);
        File.WriteAllText(Path.Combine(sourcePath, "file.txt"), "Test content");
        ZipFile.CreateFromDirectory(sourcePath, archivePath);

        _resource.Set(new ExpandSchema { ArchivePath = archivePath, DestinationPath = destinationPath });
        File.WriteAllText(Path.Combine(destinationPath, "extra.txt"), "Extra content");

        var testResult = _resource.Test(new ExpandSchema { ArchivePath = archivePath, DestinationPath = destinationPath });

        testResult.ActualState.InDesiredState.Should().BeTrue();
    }

    [Fact]
    public void Test_ReturnsTrue_WhenEmptyArchiveAndEmptyDestination()
    {
        var archivePath = GetTempPath("empty-test.zip");
        var destinationPath = GetTempPath("dest-empty-test");

        using (var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create))
        {
        }

        Directory.CreateDirectory(destinationPath);

        var testResult = _resource.Test(new ExpandSchema { ArchivePath = archivePath, DestinationPath = destinationPath });

        testResult.ActualState.InDesiredState.Should().BeTrue();
    }

    [Fact]
    public void Test_ReturnsTrue_WhenNestedDirectoriesMatch()
    {
        var archivePath = GetTempPath("nested-test.zip");
        var sourcePath = GetTempPath("source-nested-test");
        var destinationPath = GetTempPath("dest-nested-test");

        var nestedSource = Path.Combine(sourcePath, "subdir");
        Directory.CreateDirectory(nestedSource);
        File.WriteAllText(Path.Combine(nestedSource, "nested.txt"), "Nested content");
        ZipFile.CreateFromDirectory(sourcePath, archivePath);

        _resource.Set(new ExpandSchema { ArchivePath = archivePath, DestinationPath = destinationPath });

        var testResult = _resource.Test(new ExpandSchema { ArchivePath = archivePath, DestinationPath = destinationPath });

        testResult.ActualState.InDesiredState.Should().BeTrue();
    }
}
