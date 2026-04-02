// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Resource;

using Xunit;

using NonAotResource = TestResource.NonAot.Resource;
using NonAotSchema = TestResource.NonAot.Schema;
using NonAotContext = TestResource.NonAot.SourceGenerationContext;

namespace TestResource.NonAot.Tests;

[Trait("Category", "Integration")]
public sealed class ResourceTests
{
    private readonly NonAotResource _resource = new(NonAotContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void GetSchema_ContainsPathProperty()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.GetProperty("properties").TryGetProperty("path", out _).Should().BeTrue();
    }

    [Fact]
    public void GetSchema_ContainsExistProperty()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.GetProperty("properties").TryGetProperty("_exist", out _).Should().BeTrue();
    }

    [Fact]
    public void GetSchema_PathIsRequired()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        var required = doc.RootElement.GetProperty("required");
        required.EnumerateArray().Select(e => e.GetString()).Should().Contain("path");
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectType()
    {
        var attr = typeof(NonAotResource).GetCustomAttribute<DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Test/NonAotFile");
    }

    [Fact]
    public void DscResourceAttribute_HasDescription()
    {
        var attr = typeof(NonAotResource).GetCustomAttribute<DscResourceAttribute>();

        attr!.Description.Should().Be("Non-AOT test resource for file existence.");
    }

    [Fact]
    public void DscResourceAttribute_HasTags()
    {
        var attr = typeof(NonAotResource).GetCustomAttribute<DscResourceAttribute>();

        attr!.Tags.Should().Contain("test");
        attr.Tags.Should().Contain("file");
        attr.Tags.Should().Contain("nonAot");
    }

    [Fact]
    public void Get_ExistingFile_ReturnsExistNull()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "content");
        try
        {
            var result = _resource.Get(new NonAotSchema { Path = tempFile });

            result.Exist.Should().BeNull();
            result.Path.Should().Be(tempFile);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_NonExistentFile_ReturnsExistFalse()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.txt");

        var result = _resource.Get(new NonAotSchema { Path = nonExistentPath });

        result.Exist.Should().BeFalse();
        result.Path.Should().Be(nonExistentPath);
    }

    [Fact]
    public void Get_TriggerJsonException_ThrowsJsonException()
    {
        var act = () => _resource.Get(new NonAotSchema { Path = "trigger-json-exception.txt" });

        act.Should().Throw<JsonException>();
    }

    [Fact]
    public void Get_TriggerGenericException_ThrowsInvalidOperationException()
    {
        var act = () => _resource.Get(new NonAotSchema { Path = "trigger-generic-exception.txt" });

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Get_TriggerIOException_ThrowsIOException()
    {
        var act = () => _resource.Get(new NonAotSchema { Path = "trigger-io-exception.txt" });

        act.Should().Throw<IOException>();
    }

    [Fact]
    public void Get_TriggerDirectoryNotFoundException_ThrowsDirectoryNotFoundException()
    {
        var act = () => _resource.Get(new NonAotSchema { Path = "trigger-directory-not-found.txt" });

        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void Get_TriggerUnauthorizedAccess_ThrowsUnauthorizedAccessException()
    {
        var act = () => _resource.Get(new NonAotSchema { Path = "trigger-unauthorized-access.txt" });

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Test_ExistingFile_ReturnsInDesiredState()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "content");
        try
        {
            var result = _resource.Test(new NonAotSchema { Path = tempFile });

            result.ActualState.InDesiredState.Should().BeTrue();
            result.DifferingProperties.Should().BeEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Test_NonExistentFile_ReturnsNotInDesiredState()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.txt");

        var result = _resource.Test(new NonAotSchema { Path = nonExistentPath });

        result.ActualState.InDesiredState.Should().BeFalse();
        result.DifferingProperties.Should().Contain("_exist");
    }

    [Fact]
    public void Test_FileExistsButWantDeleted_ReturnsNotInDesiredState()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "content");
        try
        {
            var result = _resource.Test(new NonAotSchema { Path = tempFile, Exist = false });

            result.ActualState.InDesiredState.Should().BeFalse();
            result.DifferingProperties.Should().Contain("_exist");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_NonExistentFile_CreatesFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        try
        {
            _resource.Set(new NonAotSchema { Path = tempFile });

            File.Exists(tempFile).Should().BeTrue();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_NonExistentFile_ReportsChangedProperties()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        try
        {
            var result = _resource.Set(new NonAotSchema { Path = tempFile });

            result!.ChangedProperties.Should().Contain("_exist");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_ExistingFileWithoutExist_ReportsNoChanges()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "content");
        try
        {
            var result = _resource.Set(new NonAotSchema { Path = tempFile });

            File.Exists(tempFile).Should().BeTrue();
            result!.ChangedProperties.Should().BeEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_ExistingFileWithExistFalse_DeletesFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "content");
        try
        {
            var result = _resource.Set(new NonAotSchema { Path = tempFile, Exist = false });

            File.Exists(tempFile).Should().BeFalse();
            result!.ActualState.Exist.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Delete_ExistingFile_RemovesFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "content");
        try
        {
            _resource.Delete(new NonAotSchema { Path = tempFile });

            File.Exists(tempFile).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Delete_NonExistentFile_DoesNotThrow()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.txt");

        var act = () => _resource.Delete(new NonAotSchema { Path = nonExistentPath });

        act.Should().NotThrow();
    }

    [Fact]
    public void Export_AllMatchingFiles_ReturnsFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"export_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var originalExportDir = Environment.GetEnvironmentVariable("TEST_EXPORT_DIR");
        try
        {
            var file1 = Path.Combine(tempDir, "test-export1.txt");
            var file2 = Path.Combine(tempDir, "test-export2.txt");
            File.WriteAllText(file1, "export test 1");
            File.WriteAllText(file2, "export test 2");
            Environment.SetEnvironmentVariable("TEST_EXPORT_DIR", tempDir);

            var results = _resource.Export(null).ToList();

            results.Count.Should().BeGreaterThanOrEqualTo(2);
            results.Select(r => r.Path).Should().Contain(file1);
            results.Select(r => r.Path).Should().Contain(file2);
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_EXPORT_DIR", originalExportDir);
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Export_Files_HaveNullExist()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"export_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var originalExportDir = Environment.GetEnvironmentVariable("TEST_EXPORT_DIR");
        try
        {
            File.WriteAllText(Path.Combine(tempDir, "test-file.txt"), "content");
            Environment.SetEnvironmentVariable("TEST_EXPORT_DIR", tempDir);

            var results = _resource.Export(null).ToList();

            foreach (var result in results)
            {
                result.Exist.Should().BeNull();
            }
        }
        finally
        {
            Environment.SetEnvironmentVariable("TEST_EXPORT_DIR", originalExportDir);
            Directory.Delete(tempDir, true);
        }
    }
}
