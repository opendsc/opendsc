// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Resource;

using Xunit;

using MultiContext = TestResource.Multi.SourceGenerationContext;

namespace TestResource.Multi.Tests;

[Trait("Category", "Integration")]
public sealed class FileResourceTests
{
    private readonly FileResource _resource = new(MultiContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void GetSchema_ContainsPathAndContentProperties()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        var props = doc.RootElement.GetProperty("properties");
        props.TryGetProperty("path", out _).Should().BeTrue();
        props.TryGetProperty("content", out _).Should().BeTrue();
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectType()
    {
        var attr = typeof(FileResource).GetCustomAttribute<DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("TestResource.Multi/File");
    }

    [Fact]
    public void DscResourceAttribute_HasDescription()
    {
        var attr = typeof(FileResource).GetCustomAttribute<DscResourceAttribute>();

        attr!.Description.Should().Be("Manages file content");
    }

    [Fact]
    public void DscResourceAttribute_HasTags()
    {
        var attr = typeof(FileResource).GetCustomAttribute<DscResourceAttribute>();

        attr!.Tags.Should().Contain("file");
        attr.Tags.Should().Contain("content");
    }

    [Fact]
    public void Get_NonExistentFile_ReturnsExistFalse()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.txt");

        var result = _resource.Get(new FileSchema { Path = nonExistentPath });

        result.Exist.Should().BeFalse();
        result.Path.Should().Be(nonExistentPath);
        result.Content.Should().BeNull();
    }

    [Fact]
    public void Get_ExistingFile_ReturnsContent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "hello world");
        try
        {
            var result = _resource.Get(new FileSchema { Path = tempFile });

            result.Exist.Should().BeNull();
            result.Path.Should().Be(tempFile);
            result.Content.Should().Be("hello world");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Get_TriggerGenericException_ThrowsInvalidOperationException()
    {
        var act = () => _resource.Get(new FileSchema { Path = "trigger-generic-exception.txt" });

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Get_TriggerIOException_ThrowsIOException()
    {
        var act = () => _resource.Get(new FileSchema { Path = "trigger-io-exception.txt" });

        act.Should().Throw<IOException>();
    }

    [Fact]
    public void Get_TriggerDirectoryNotFoundException_ThrowsDirectoryNotFoundException()
    {
        var act = () => _resource.Get(new FileSchema { Path = "trigger-directory-not-found.txt" });

        act.Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void Get_TriggerUnauthorizedAccess_ThrowsUnauthorizedAccessException()
    {
        var act = () => _resource.Get(new FileSchema { Path = "trigger-unauthorized-access.txt" });

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void Test_ExistingFileWithMatchingContent_ReturnsInDesiredState()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test content");
        try
        {
            var result = _resource.Test(new FileSchema { Path = tempFile, Content = "test content" });

            result.DifferingProperties.Should().BeNullOrEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Test_ExistingFileWithDifferentContent_ReturnsDifferingProperty()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "old content");
        try
        {
            var result = _resource.Test(new FileSchema { Path = tempFile, Content = "new content" });

            result.DifferingProperties.Should().Contain(nameof(FileSchema.Content));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Test_NonExistentFile_ReturnsDifferingProperty()
    {
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.txt");

        var result = _resource.Test(new FileSchema { Path = nonExistentPath });

        result.DifferingProperties.Should().Contain(nameof(FileSchema.Exist));
    }

    [Fact]
    public void Set_NewFile_CreatesWithContent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        try
        {
            _resource.Set(new FileSchema { Path = tempFile, Content = "new content" });

            File.Exists(tempFile).Should().BeTrue();
            File.ReadAllText(tempFile).Should().Be("new content");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_ExistingFile_UpdatesContent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "old content");
        try
        {
            _resource.Set(new FileSchema { Path = tempFile, Content = "updated content" });

            File.ReadAllText(tempFile).Should().Be("updated content");
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
            _resource.Set(new FileSchema { Path = tempFile, Exist = false });

            File.Exists(tempFile).Should().BeFalse();
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
            _resource.Delete(new FileSchema { Path = tempFile });

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

        var act = () => _resource.Delete(new FileSchema { Path = nonExistentPath });

        act.Should().NotThrow();
    }
}
