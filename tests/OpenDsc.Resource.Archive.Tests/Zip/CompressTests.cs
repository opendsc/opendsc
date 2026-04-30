// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IO.Compression;

using AwesomeAssertions;

using Xunit;

using CompressResource = OpenDsc.Resource.Archive.Zip.Compress.Resource;
using CompressSchema = OpenDsc.Resource.Archive.Zip.Compress.Schema;

namespace OpenDsc.Resource.Archive.Tests.Zip;

[Trait("Category", "Integration")]
public sealed class CompressTests : IDisposable
{
    private readonly CompressResource _resource = new(OpenDsc.Resource.Archive.SourceGenerationContext.Default);
    private readonly string _workPath = Path.Combine(Path.GetTempPath(), "OpenDsc.Resource.Archive.Tests", Guid.NewGuid().ToString("N"));

    public CompressTests()
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

    private string GetTempPath(string name) => Path.Combine(_workPath, name);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = System.Text.Json.JsonDocument.Parse(schemaJson);

        doc.RootElement.ValueKind.Should().Be(System.Text.Json.JsonValueKind.Object);
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectTypeAndVersion()
    {
        var attr = typeof(CompressResource).GetCustomAttributes(typeof(DscResourceAttribute), false)
            .OfType<DscResourceAttribute>().SingleOrDefault();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Archive.Zip/Compress");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_ReturnsSameArchiveInfo()
    {
        var archivePath = GetTempPath("out.zip");
        var sourcePath = GetTempPath("source");

        var state = _resource.Get(new CompressSchema
        {
            ArchivePath = archivePath,
            SourcePath = sourcePath,
            CompressionLevel = CompressionLevel.Fastest
        });

        state.ArchivePath.Should().Be(archivePath);
        state.SourcePath.Should().Be(sourcePath);
        state.CompressionLevel.Should().Be(CompressionLevel.Fastest);
    }

    [Fact]
    public void Set_Directory_CreatesZipArchive()
    {
        var archivePath = GetTempPath("check.zip");
        var sourcePath = GetTempPath("source-dir");
        Directory.CreateDirectory(sourcePath);

        File.WriteAllText(Path.Combine(sourcePath, "file1.txt"), "Test1");
        File.WriteAllText(Path.Combine(sourcePath, "file2.txt"), "Test2");

        _resource.Set(new CompressSchema
        {
            ArchivePath = archivePath,
            SourcePath = sourcePath,
            CompressionLevel = CompressionLevel.Optimal
        });

        File.Exists(archivePath).Should().BeTrue();

        using var archive = ZipFile.OpenRead(archivePath);
        archive.Entries.Count.Should().Be(2);
        archive.Entries.Select(e => e.FullName).Should().Contain("file1.txt");
        archive.Entries.Select(e => e.FullName).Should().Contain("file2.txt");
    }

    [Fact]
    public void Set_File_CreatesZipArchiveWithSingleEntry()
    {
        var archivePath = GetTempPath("single.zip");
        var sourcePath = GetTempPath("single-file.txt");

        File.WriteAllText(sourcePath, "single content");

        _resource.Set(new CompressSchema
        {
            ArchivePath = archivePath,
            SourcePath = sourcePath,
            CompressionLevel = CompressionLevel.NoCompression
        });

        File.Exists(archivePath).Should().BeTrue();

        using var archive = ZipFile.OpenRead(archivePath);
        archive.Entries.Count.Should().Be(1);
        archive.Entries[0].Name.Should().Be(Path.GetFileName(sourcePath));
    }

    [Fact]
    public void Set_IsIdempotent_WhenRunTwiceOnSameSource()
    {
        var archivePath = GetTempPath("idempotent.zip");
        var sourcePath = GetTempPath("source-idempotent");
        Directory.CreateDirectory(sourcePath);

        File.WriteAllText(Path.Combine(sourcePath, "file.txt"), "content");

        _resource.Set(new CompressSchema { ArchivePath = archivePath, SourcePath = sourcePath });
        _resource.Set(new CompressSchema { ArchivePath = archivePath, SourcePath = sourcePath });

        File.Exists(archivePath).Should().BeTrue();

        using var archive = ZipFile.OpenRead(archivePath);
        archive.Entries.Count.Should().Be(1);
    }

    [Fact]
    public void Test_ReturnsDesiredState_AndDetectsSourceChange()
    {
        var archivePath = GetTempPath("test.zip");
        var sourcePath = GetTempPath("source-test");
        Directory.CreateDirectory(sourcePath);
        File.WriteAllText(Path.Combine(sourcePath, "file.txt"), "contentA");

        _resource.Set(new CompressSchema { ArchivePath = archivePath, SourcePath = sourcePath });

        var testResult = _resource.Test(new CompressSchema { ArchivePath = archivePath, SourcePath = sourcePath });
        testResult.ActualState.InDesiredState.Should().BeTrue();

        File.WriteAllText(Path.Combine(sourcePath, "file.txt"), "contentB");

        var testResultAfterChange = _resource.Test(new CompressSchema { ArchivePath = archivePath, SourcePath = sourcePath });
        testResultAfterChange.ActualState.InDesiredState.Should().BeFalse();
    }

    [Fact]
    public void Test_ReturnsFalse_WhenArchiveMissing()
    {
        var archivePath = GetTempPath("missing.zip");
        var sourcePath = GetTempPath("source-missing");
        Directory.CreateDirectory(sourcePath);

        var testResult = _resource.Test(new CompressSchema { ArchivePath = archivePath, SourcePath = sourcePath });

        testResult.ActualState.InDesiredState.Should().BeFalse();
    }
}
