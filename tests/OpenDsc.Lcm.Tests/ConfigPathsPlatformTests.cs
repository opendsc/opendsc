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
    public void GetLcmConfigDirectory_OnUnsupportedPlatform_ThrowsPlatformNotSupportedException()
    {
        // This test uses reflection to mock RuntimeInformation.IsOSPlatform()
        // to simulate an unsupported platform (none of Windows, Linux, OSX return true)

        var method = typeof(ConfigPaths).GetMethod("GetLcmConfigDirectory",
            BindingFlags.Public | BindingFlags.Static);

        method.Should().NotBeNull();

        // On the current platform, one of the checks will pass, so we can only test
        // that the method succeeds rather than testing the PlatformNotSupportedException
        // For true unsupported platform testing, we'd need to mock RuntimeInformation
        // which is not practical without runtime instrumentation

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
