// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Lcm.Tests;

[Trait("Category", "Unit")]
public class ConfigPathsPlatformTests
{
    [Fact]
    public void GetLcmConfigDirectory_OnCurrentPlatform_ReturnsValidPath()
    {
        // Verifies that GetLcmConfigDirectory returns a valid path on the current platform
        // (Unable to test PlatformNotSupportedException without runtime instrumentation
        // to mock RuntimeInformation.IsOSPlatform())

        var method = typeof(ConfigPaths).GetMethod("GetLcmConfigDirectory",
            BindingFlags.Public | BindingFlags.Static);

        method.Should().NotBeNull();

        var result = method!.Invoke(null, null);
        result.Should().BeOfType<string>();
        ((string)result!).Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetLcmConfigDirectory_AlwaysReturnsValidPlatformPath()
    {
        // Verify the returned path matches the expected pattern for the current platform
        var path = ConfigPaths.GetLcmConfigDirectory();

        Path.IsPathRooted(path).Should().BeTrue();
        path.Should().NotBeNullOrWhiteSpace();

        // Verify it's one of the known platform paths
        if (OperatingSystem.IsWindows())
        {
            path.ToLowerInvariant().Should().Contain("programdata");
            path.ToLowerInvariant().Should().Contain("opendsc");
        }
        else if (OperatingSystem.IsLinux())
        {
            path.Should().StartWith("/etc");
            path.Should().Contain("opendsc");
        }
        else if (OperatingSystem.IsMacOS())
        {
            path.Should().StartWith("/Library");
            path.Should().Contain("OpenDSC");
        }
    }

    [Fact]
    public void GetLcmConfigPath_ReturnsValidPath()
    {
        var configPath = ConfigPaths.GetLcmConfigPath();

        configPath.Should().NotBeNullOrWhiteSpace();
        Path.IsPathRooted(configPath).Should().BeTrue();
        Path.GetFileName(configPath).Should().Be("appsettings.json");
    }

    [Fact]
    public void GetLcmConfigPath_IncludesLcmDirectory()
    {
        var configPath = ConfigPaths.GetLcmConfigPath();

        configPath.ToLowerInvariant().Should().Contain("lcm");
    }

    [Fact]
    public void GetLcmConfigDirectory_IncludesOpenDscFolder()
    {
        var configDir = ConfigPaths.GetLcmConfigDirectory();

        configDir.ToLowerInvariant().Should().Contain("opendsc");
    }

    [Fact]
    public void GetLcmConfigPath_IsSubdirectoryOfConfigDirectory()
    {
        var configPath = ConfigPaths.GetLcmConfigPath();
        var configDir = ConfigPaths.GetLcmConfigDirectory();

        var normalizedConfigPath = Path.GetFullPath(configPath);
        var normalizedConfigDir = Path.GetFullPath(configDir);

        normalizedConfigPath.Should().StartWith(normalizedConfigDir);
    }
}
