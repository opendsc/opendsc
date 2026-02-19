// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Lcm.Tests;

public sealed class ConfigPathsTests
{
    [Fact]
    public void GetLcmConfigDirectory_ShouldReturnValidPath()
    {
        var configDir = ConfigPaths.GetLcmConfigDirectory();

        configDir.Should().NotBeNullOrWhiteSpace();
        Path.IsPathRooted(configDir).Should().BeTrue();
    }

    [Fact]
    public void GetLcmConfigDirectory_ShouldBePlatformSpecific()
    {
        var configDir = ConfigPaths.GetLcmConfigDirectory();

        if (OperatingSystem.IsWindows())
        {
            configDir.Should().Contain("ProgramData");
        }
        else if (OperatingSystem.IsLinux())
        {
            configDir.Should().StartWith("/etc");
        }
        else if (OperatingSystem.IsMacOS())
        {
            configDir.Should().StartWith("/Library");
        }
    }

    [Fact]
    public void GetLcmConfigDirectory_ShouldIncludeLcmFolder()
    {
        var configDir = ConfigPaths.GetLcmConfigDirectory();

        configDir.ToLowerInvariant().Should().Contain("lcm");
    }

    [Fact]
    public void GetLcmConfigDirectory_ShouldIncludeOpenDscFolder()
    {
        var configDir = ConfigPaths.GetLcmConfigDirectory();

        configDir.ToLowerInvariant().Should().Contain("opendsc");
    }
}
