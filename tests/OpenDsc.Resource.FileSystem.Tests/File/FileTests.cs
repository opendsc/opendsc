// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IO;
using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

using FileResource = OpenDsc.Resource.FileSystem.File.Resource;
using FileSchema = OpenDsc.Resource.FileSystem.File.Schema;

namespace OpenDsc.Resource.FileSystem.Tests.File;

[Trait("Category", "Integration")]
public sealed class FileTests
{
    private readonly FileResource _resource = new(SourceGenerationContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.GetProperty("$schema").GetString()
            .Should().Be("https://json-schema.org/draft/2020-12/schema");
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectTypeAndVersion()
    {
        var attr = typeof(FileResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.FileSystem/File");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NonExistentFile_ReturnsExistFalse()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}.txt");

        var result = _resource.Get(new FileSchema { Path = tempFile });

        result.Exist.Should().BeFalse();
        result.Path.Should().Be(tempFile);
    }

    [Fact]
    public void Get_ExistingFile_ReturnsContent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"existing_{Guid.NewGuid():N}.txt");
        System.IO.File.WriteAllText(tempFile, "hello world");
        try
        {
            var result = _resource.Get(new FileSchema { Path = tempFile });

            result.Path.Should().Be(tempFile);
            result.Content.Should().Be("hello world");
        }
        finally
        {
            System.IO.File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_NewFile_CreatesWithContent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setnew_{Guid.NewGuid():N}.txt");
        try
        {
            _resource.Set(new FileSchema { Path = tempFile, Content = "content123" });

            System.IO.File.Exists(tempFile).Should().BeTrue();
            System.IO.File.ReadAllText(tempFile).Should().Be("content123");
        }
        finally
        {
            System.IO.File.Delete(tempFile);
        }
    }

    [Fact]
    public void Set_ExistingFile_UpdatesContent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setupdate_{Guid.NewGuid():N}.txt");
        System.IO.File.WriteAllText(tempFile, "old");
        try
        {
            _resource.Set(new FileSchema { Path = tempFile, Content = "updated" });

            System.IO.File.ReadAllText(tempFile).Should().Be("updated");
        }
        finally
        {
            System.IO.File.Delete(tempFile);
        }
    }

    [Fact]
    public void Delete_ExistingFile_RemovesFile()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"delete_{Guid.NewGuid():N}.txt");
        System.IO.File.WriteAllText(tempFile, "remove me");

        _resource.Delete(new FileSchema { Path = tempFile });

        System.IO.File.Exists(tempFile).Should().BeFalse();

        var result = _resource.Get(new FileSchema { Path = tempFile });
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Delete_NonExistentFile_DoesNotThrow()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"delete_nonexistent_{Guid.NewGuid():N}.txt");

        var act = () => _resource.Delete(new FileSchema { Path = tempFile });

        act.Should().NotThrow();
    }
}
