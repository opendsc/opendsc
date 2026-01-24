// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using FluentAssertions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using OpenDsc.Lcm;

using Xunit;

namespace OpenDsc.Lcm.Tests;

[Trait("Category", "Unit")]
public class LcmWorkerTests
{
    [Fact]
    public void ConfigurationModeInterval_DefaultValue()
    {
        var config = new LcmConfig();
        config.ConfigurationModeInterval.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void ConfigurationMode_DefaultValue()
    {
        var config = new LcmConfig();
        config.ConfigurationMode.Should().Be(ConfigurationMode.Monitor);
    }

    [Fact]
    public void LcmConfig_AcceptsCustomInterval()
    {
        var config = new LcmConfig
        {
            ConfigurationModeInterval = TimeSpan.FromMinutes(5)
        };

        config.ConfigurationModeInterval.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Theory]
    [InlineData(ConfigurationMode.Monitor)]
    [InlineData(ConfigurationMode.Remediate)]
    public void LcmConfig_AcceptsAllModes(ConfigurationMode mode)
    {
        var config = new LcmConfig
        {
            ConfigurationMode = mode
        };

        config.ConfigurationMode.Should().Be(mode);
    }

    [Fact]
    public void LcmConfig_AcceptsConfigurationPath()
    {
        var config = new LcmConfig
        {
            ConfigurationPath = "/test/config.yaml"
        };

        config.ConfigurationPath.Should().Be("/test/config.yaml");
    }

    [Fact]
    public void LcmConfig_AcceptsDscExecutablePath()
    {
        var config = new LcmConfig
        {
            DscExecutablePath = "/usr/bin/dsc"
        };

        config.DscExecutablePath.Should().Be("/usr/bin/dsc");
    }

    [Theory]
    [InlineData(ConfigurationSource.Local)]
    [InlineData(ConfigurationSource.Pull)]
    public void LcmConfig_AcceptsAllSources(ConfigurationSource source)
    {
        var config = new LcmConfig
        {
            ConfigurationSource = source
        };

        config.ConfigurationSource.Should().Be(source);
    }

    [Fact]
    public void LcmConfig_DefaultSourceIsLocal()
    {
        var config = new LcmConfig();
        config.ConfigurationSource.Should().Be(ConfigurationSource.Local);
    }

    [Fact]
    public void PullServerSettings_AcceptsServerUrl()
    {
        var settings = new PullServerSettings
        {
            ServerUrl = "https://server.example.com"
        };

        settings.ServerUrl.Should().Be("https://server.example.com");
    }

    [Fact]
    public void PullServerSettings_AcceptsRegistrationKey()
    {
        var settings = new PullServerSettings
        {
            RegistrationKey = "test-key-123"
        };

        settings.RegistrationKey.Should().Be("test-key-123");
    }

    [Fact]
    public void PullServerSettings_DefaultReportComplianceIsTrue()
    {
        var settings = new PullServerSettings();
        settings.ReportCompliance.Should().BeTrue();
    }
}
