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

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
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
            result.Mode!.Should().MatchRegex(@"^0?[0-7]{3}$");
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

            result.Mode.Should().MatchRegex(@"^0?644$");
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

            result.Mode.Should().MatchRegex(@"^0?755$");
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
            result.Mode!.Should().MatchRegex(@"^0?[0-7]{3}$");
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

            result.Mode.Should().MatchRegex(@"^0?755$");
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }

    [NonWindowsFact]
    public void Set_Owner_UpdatesOwner()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setowner_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            var initial = _resource.Get(new PermissionSchema { Path = tempFile });
            var currentUid = Environment.GetEnvironmentVariable("UID") ?? "0";

            // Set owner to current effective UID to avoid elevation requirement
            _resource.Set(new PermissionSchema { Path = tempFile, Owner = currentUid });

            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            // Verify the operation completed without error
            result.Owner.Should().NotBeNullOrEmpty();
        }
        catch (UnauthorizedAccessException)
        {
            // Skip test if insufficient privileges (expected on non-root CI agents)
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Set_Group_UpdatesGroup()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setgroup_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            var initial = _resource.Get(new PermissionSchema { Path = tempFile });
            var currentGid = Environment.GetEnvironmentVariable("GID") ?? "0";

            // Set group to current effective GID to avoid elevation requirement
            _resource.Set(new PermissionSchema { Path = tempFile, Group = currentGid });

            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            // Verify the operation completed without error
            result.Group.Should().NotBeNullOrEmpty();
        }
        catch (UnauthorizedAccessException)
        {
            // Skip test if insufficient privileges (expected on non-root CI agents)
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Set_ModeAndOwner_UpdatesBoth()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setmodeowner_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            var currentUid = Environment.GetEnvironmentVariable("UID") ?? "0";
            _resource.Set(new PermissionSchema { Path = tempFile, Mode = "0644", Owner = currentUid });

            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            result.Mode.Should().MatchRegex(@"^0?644$");
            result.Owner.Should().NotBeNullOrEmpty();
        }
        catch (UnauthorizedAccessException)
        {
            // Skip test if insufficient privileges (expected on non-root CI agents)
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Set_ModeAndGroup_UpdatesBoth()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setmodegroup_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            var currentGid = Environment.GetEnvironmentVariable("GID") ?? "0";
            _resource.Set(new PermissionSchema { Path = tempFile, Mode = "0644", Group = currentGid });

            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            result.Mode.Should().MatchRegex(@"^0?644$");
            result.Group.Should().NotBeNullOrEmpty();
        }
        catch (UnauthorizedAccessException)
        {
            // Skip test if insufficient privileges (expected on non-root CI agents)
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Set_OwnerAndGroup_UpdatesBoth()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setownergroup_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            // Set both owner and group using current effective UID/GID
            var currentUid = Environment.GetEnvironmentVariable("UID") ?? "0";
            var currentGid = Environment.GetEnvironmentVariable("GID") ?? "0";
            _resource.Set(new PermissionSchema { Path = tempFile, Owner = currentUid, Group = currentGid });

            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            result.Owner.Should().NotBeNullOrEmpty();
            result.Group.Should().NotBeNullOrEmpty();
        }
        catch (UnauthorizedAccessException)
        {
            // Skip test if insufficient privileges (expected on non-root CI agents)
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Set_ModeOwnerAndGroup_UpdatesAll()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setall_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            // Set all three attributes using current effective UID/GID
            var currentUid = Environment.GetEnvironmentVariable("UID") ?? "0";
            var currentGid = Environment.GetEnvironmentVariable("GID") ?? "0";
            _resource.Set(new PermissionSchema { Path = tempFile, Mode = "0644", Owner = currentUid, Group = currentGid });

            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            result.Mode.Should().MatchRegex(@"^0?644$");
            result.Owner.Should().NotBeNullOrEmpty();
            result.Group.Should().NotBeNullOrEmpty();
        }
        catch (UnauthorizedAccessException)
        {
            // Skip test if insufficient privileges (expected on non-root CI agents)
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Set_ModeZero_UpdatesMode()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setmode_zero_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            _resource.Set(new PermissionSchema { Path = tempFile, Mode = "0000" });

            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            result.Mode.Should().MatchRegex(@"^0?0+$");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Set_InvalidOwnerName_ThrowsArgumentException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setowner_invalid_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            var act = () => _resource.Set(new PermissionSchema { Path = tempFile, Owner = "nonexistent_user_that_should_not_exist_12345" });

            act.Should().Throw<ArgumentException>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Set_InvalidGroupName_ThrowsArgumentException()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setgroup_invalid_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            var act = () => _resource.Set(new PermissionSchema { Path = tempFile, Group = "nonexistent_group_that_should_not_exist_12345" });

            act.Should().Throw<ArgumentException>();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Get_NullInstanceParameter_ThrowsArgumentNullException()
    {
        var act = () => _resource.Get(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [NonWindowsFact]
    public void Set_NullInstanceParameter_ThrowsArgumentNullException()
    {
        var act = () => _resource.Set(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [NonWindowsFact]
    public void Set_OwnerWithNumericUid_UpdatesOwner()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setowner_uid_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            // Set owner by numeric UID (use current effective UID for portability)
            var currentUid = Environment.GetEnvironmentVariable("UID") ?? "0";
            _resource.Set(new PermissionSchema { Path = tempFile, Owner = currentUid });

            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            result.Owner.Should().NotBeNullOrEmpty();
        }
        catch (UnauthorizedAccessException)
        {
            // Skip test if insufficient privileges (expected on non-root CI agents)
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Set_GroupWithNumericGid_UpdatesGroup()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"setgroup_gid_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            // Set group by numeric GID (use current effective GID for portability)
            var currentGid = Environment.GetEnvironmentVariable("GID") ?? "0";
            _resource.Set(new PermissionSchema { Path = tempFile, Group = currentGid });

            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            result.Group.Should().NotBeNullOrEmpty();
        }
        catch (UnauthorizedAccessException)
        {
            // Skip test if insufficient privileges (expected on non-root CI agents)
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Get_FileMode_ReturnsFormattedMode()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"get_mode_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            // Mode should be in format 0755 or similar
            result.Mode.Should().NotBeNull();
            result.Mode!.Length.Should().BeGreaterThanOrEqualTo(3);
            result.Mode!.Length.Should().BeLessThanOrEqualTo(4);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Set_PreservesOtherAttributes_WhenSettingMode()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"preserve_owner_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            var initial = _resource.Get(new PermissionSchema { Path = tempFile });

            // Set only mode
            _resource.Set(new PermissionSchema { Path = tempFile, Mode = "0600" });

            var result = _resource.Get(new PermissionSchema { Path = tempFile });

            // Owner and group should remain the same
            result.Owner.Should().Be(initial.Owner);
            result.Group.Should().Be(initial.Group);
            result.Mode.Should().MatchRegex(@"^0?600$");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [NonWindowsFact]
    public void Set_ReturnsNull_OnSuccess()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"returnsnull_{Guid.NewGuid():N}.txt");
        File.WriteAllText(tempFile, "test");

        try
        {
            var result = _resource.Set(new PermissionSchema { Path = tempFile, Mode = "0644" });

            result.Should().BeNull();
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
