// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Server.Tests;

[Trait("Category", "Unit")]
public class ServerPathsTests
{
    [Fact]
    public void GetServerConfigDirectory_ReturnsNonEmpty()
    {
        ServerPaths.GetServerConfigDirectory().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetServerConfigPath_EndsWithAppsettingsJson()
    {
        ServerPaths.GetServerConfigPath().Should().EndWith("appsettings.json");
    }

    [Fact]
    public void GetServerConfigPath_ContainsConfigDirectory()
    {
        var configDir = ServerPaths.GetServerConfigDirectory();
        ServerPaths.GetServerConfigPath().Should().StartWith(configDir);
    }

    [Fact]
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public void GetServerConfigDirectory_OnWindows_ContainsProgramData()
    {
        if (!OperatingSystem.IsWindows()) return;

        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        ServerPaths.GetServerConfigDirectory().Should().StartWith(programData);
    }

    [Fact]
    public void GetServerConfigDirectory_OnLinux_ReturnsExpectedPath()
    {
        if (!OperatingSystem.IsLinux()) return;

        ServerPaths.GetServerConfigDirectory().Should().Be("/etc/opendsc/server");
    }

    [Fact]
    public void GetServerConfigDirectory_OnMacOs_ReturnsExpectedPath()
    {
        if (!OperatingSystem.IsMacOS()) return;

        ServerPaths.GetServerConfigDirectory().Should().Be("/Library/Preferences/OpenDSC/Server");
    }
}
