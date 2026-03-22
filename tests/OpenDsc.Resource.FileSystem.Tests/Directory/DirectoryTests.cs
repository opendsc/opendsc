// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IO;
using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

using DirectoryResource = OpenDsc.Resource.FileSystem.Directory.Resource;
using DirectorySchema = OpenDsc.Resource.FileSystem.Directory.Schema;

namespace OpenDsc.Resource.FileSystem.Tests.Directory;

[Trait("Category", "Integration")]
public sealed class DirectoryTests
{
    private readonly DirectoryResource _resource = new(SourceGenerationContext.Default);

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
        var attr = typeof(DirectoryResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.FileSystem/Directory");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NonExistentDirectory_ReturnsExistFalse()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}");

        var result = _resource.Get(new DirectorySchema { Path = tempDir });

        result.Exist.Should().BeFalse();
        result.Path.Should().Be(tempDir);
    }

    [Fact]
    public void Get_ExistingDirectory_ReturnsPath()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"existing_{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(tempDir);

        try
        {
            var result = _resource.Get(new DirectorySchema { Path = tempDir });

            result.Path.Should().Be(tempDir);
            result.Exist.Should().BeNull();
        }
        finally
        {
            System.IO.Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Set_NewDirectory_CreatesDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"setnew_{Guid.NewGuid():N}");

        try
        {
            _resource.Set(new DirectorySchema { Path = tempDir });

            System.IO.Directory.Exists(tempDir).Should().BeTrue();
        }
        finally
        {
            System.IO.Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Set_NestedDirectory_CreatesParents()
    {
        var rootDir = Path.Combine(Path.GetTempPath(), $"setnested_{Guid.NewGuid():N}");
        var tempDir = Path.Combine(rootDir, "a", "b", "c");

        try
        {
            _resource.Set(new DirectorySchema { Path = tempDir });

            System.IO.Directory.Exists(tempDir).Should().BeTrue();
        }
        finally
        {
            if (System.IO.Directory.Exists(rootDir))
            {
                System.IO.Directory.Delete(rootDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Set_WithSourcePath_CopiesFiles()
    {
        var sourceDir = Path.Combine(Path.GetTempPath(), $"source_{Guid.NewGuid():N}");
        var filePath = Path.Combine(sourceDir, "test.txt");
        var targetDir = Path.Combine(Path.GetTempPath(), $"target_{Guid.NewGuid():N}");

        System.IO.Directory.CreateDirectory(sourceDir);
        System.IO.File.WriteAllText(filePath, "hello");

        try
        {
            _resource.Set(new DirectorySchema { Path = targetDir, SourcePath = sourceDir });

            System.IO.Directory.Exists(targetDir).Should().BeTrue();
            System.IO.File.Exists(Path.Combine(targetDir, "test.txt")).Should().BeTrue();
            System.IO.File.ReadAllText(Path.Combine(targetDir, "test.txt")).Should().Be("hello");
        }
        finally
        {
            if (System.IO.Directory.Exists(sourceDir))
            {
                System.IO.Directory.Delete(sourceDir, recursive: true);
            }

            if (System.IO.Directory.Exists(targetDir))
            {
                System.IO.Directory.Delete(targetDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Delete_ExistingDirectory_RemovesDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"delete_{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(tempDir);

        _resource.Delete(new DirectorySchema { Path = tempDir });

        System.IO.Directory.Exists(tempDir).Should().BeFalse();

        var result = _resource.Get(new DirectorySchema { Path = tempDir });
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Delete_NonExistentDirectory_DoesNotThrow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"delete_nonexistent_{Guid.NewGuid():N}");

        var act = () => _resource.Delete(new DirectorySchema { Path = tempDir });

        act.Should().NotThrow();
    }

    [Fact]
    public void Test_ExistingDirectory_NoSourcePath_InDesiredState()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_existing_{Guid.NewGuid():N}");
        System.IO.Directory.CreateDirectory(tempDir);

        try
        {
            var result = _resource.Test(new DirectorySchema { Path = tempDir });

            result.ActualState.InDesiredState.Should().BeTrue();
        }
        finally
        {
            System.IO.Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void Test_NonExistentDirectory_NotInDesiredState()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"test_nonexistent_{Guid.NewGuid():N}");

        var result = _resource.Test(new DirectorySchema { Path = tempDir });

        result.ActualState.InDesiredState.Should().BeFalse();
    }

    [Fact]
    public void Test_DirectoryWithSourcePath_MatchesSource_InDesiredState()
    {
        var sourceDir = Path.Combine(Path.GetTempPath(), $"test_source_{Guid.NewGuid():N}");
        var targetDir = Path.Combine(Path.GetTempPath(), $"test_target_{Guid.NewGuid():N}");
        var filePath = Path.Combine(sourceDir, "file.txt");

        System.IO.Directory.CreateDirectory(sourceDir);
        System.IO.File.WriteAllText(filePath, "hello");
        _resource.Set(new DirectorySchema { Path = targetDir, SourcePath = sourceDir });

        try
        {
            var result = _resource.Test(new DirectorySchema { Path = targetDir, SourcePath = sourceDir });

            result.ActualState.InDesiredState.Should().BeTrue();
        }
        finally
        {
            if (System.IO.Directory.Exists(sourceDir))
            {
                System.IO.Directory.Delete(sourceDir, recursive: true);
            }

            if (System.IO.Directory.Exists(targetDir))
            {
                System.IO.Directory.Delete(targetDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Test_DirectoryWithSourcePath_OutOfSync_NotInDesiredState()
    {
        var sourceDir = Path.Combine(Path.GetTempPath(), $"test_source_oos_{Guid.NewGuid():N}");
        var targetDir = Path.Combine(Path.GetTempPath(), $"test_target_oos_{Guid.NewGuid():N}");

        System.IO.Directory.CreateDirectory(sourceDir);
        System.IO.File.WriteAllText(Path.Combine(sourceDir, "a.txt"), "original");
        _resource.Set(new DirectorySchema { Path = targetDir, SourcePath = sourceDir });

        System.IO.File.WriteAllText(Path.Combine(sourceDir, "b.txt"), "extra");

        try
        {
            var result = _resource.Test(new DirectorySchema { Path = targetDir, SourcePath = sourceDir });

            result.ActualState.InDesiredState.Should().BeFalse();
        }
        finally
        {
            if (System.IO.Directory.Exists(sourceDir))
            {
                System.IO.Directory.Delete(sourceDir, recursive: true);
            }

            if (System.IO.Directory.Exists(targetDir))
            {
                System.IO.Directory.Delete(targetDir, recursive: true);
            }
        }
    }

    [Fact]
    public void Set_NonExistentSourcePath_ThrowsIOException()
    {
        var targetDir = Path.Combine(Path.GetTempPath(), $"set_missing_src_{Guid.NewGuid():N}");
        var sourceDir = Path.Combine(Path.GetTempPath(), $"nonexistent_src_{Guid.NewGuid():N}");

        try
        {
            var act = () => _resource.Set(new DirectorySchema { Path = targetDir, SourcePath = sourceDir });

            act.Should().Throw<IOException>();
        }
        finally
        {
            if (System.IO.Directory.Exists(targetDir))
            {
                System.IO.Directory.Delete(targetDir, recursive: true);
            }
        }
    }
}
