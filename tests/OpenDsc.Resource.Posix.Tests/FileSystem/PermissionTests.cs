// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Resource.Posix.Tests.Helpers;

using Xunit;

using PermissionResource = OpenDsc.Resource.Posix.FileSystem.Permission.Resource;
using PermissionSchema = OpenDsc.Resource.Posix.FileSystem.Permission.Schema;

namespace OpenDsc.Resource.Posix.Tests.FileSystem;

[Trait("Category", "Integration")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed class PermissionTests
{
    private readonly PermissionResource _resource = new(SourceGenerationContext.Default);

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
        var attr = typeof(PermissionResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Posix.FileSystem/Permission");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [NonWindowsFact]
    public void Get_NonExistentPath_ThrowsFileNotFoundException()
    {
        var invalidPath = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid():N}");

        var act = () => _resource.Get(new PermissionSchema { Path = invalidPath });

        act.Should().Throw<FileNotFoundException>();
    }

    [NonWindowsFact]
    public void Get_ExistingFile_ReturnsModeOwnerGroup()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"existing_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "hello");

        try
        {
            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            result.Path.Should().Be(tempFile);
            result.Mode.Should().NotBeNullOrEmpty();
            result.Mode!.Should().Match(@"^0?[0-7]{3}$");
            result.Owner.Should().NotBeNullOrEmpty();
            result.Group.Should().NotBeNullOrEmpty();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Set_Mode_UpdatesMode()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setmode_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            _resource.Set(new PermissionSchema { Path = tempFile, Mode = "0644" });

            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            result.Mode.Should().Match(@"^0?644$");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Set_ModeWithoutLeadingZero_UpdatesMode()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setmode_nolead_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            _resource.Set(new PermissionSchema { Path = tempFile, Mode = "755" });

            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            result.Mode.Should().Match(@"^0?755$");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Get_ExistingDirectory_ReturnsModeOwnerGroup()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"permdir_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var result = _resource.Get(new PermissionSchema { Path = tempDir });

            result.Path.Should().Be(tempDir);
            result.Mode.Should().NotBeNullOrEmpty();
            result.Mode!.Should().Match(@"^0?[0-7]{3}$");
            result.Owner.Should().NotBeNullOrEmpty();
            result.Group.Should().NotBeNullOrEmpty();
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [NonWindowsFact]
    public void Set_NonExistentPath_ThrowsFileNotFoundException()
    {
        var invalidPath = Path.Combine(Path.GetTempPath(), $"nonexistent_set_{Guid.NewGuid():N}");

        var act = () => _resource.Set(new PermissionSchema { Path = invalidPath, Mode = "0644" });

        act.Should().Throw<FileNotFoundException>();
    }

    [NonWindowsFact]
    public void Set_ModeOnDirectory_UpdatesMode()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"setmode_dir_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            _resource.Set(new PermissionSchema { Path = tempDir, Mode = "0755" });

            var result = _resource.Get(new PermissionSchema { Path = tempDir });

            result.Mode.Should().Match(@"^0?755$");
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }
}
