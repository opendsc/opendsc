// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Server.Tests;

[Trait("Category", "Unit")]
public class ServerConfigTests
{
    [Fact]
    public void DataDirectory_Default_ReturnsNonEmpty()
    {
        var config = new ServerConfig();
        config.DataDirectory.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void DataDirectory_Default_EndsWithData()
    {
        var config = new ServerConfig();
        config.DataDirectory.Should().EndWith("data");
    }

    [Fact]
    public void ConfigurationsDirectory_Default_NestedUnderDataDirectory()
    {
        var config = new ServerConfig();
        config.ConfigurationsDirectory.Should().StartWith(config.DataDirectory);
        config.ConfigurationsDirectory.Should().EndWith("configurations");
    }

    [Fact]
    public void ParametersDirectory_Default_NestedUnderDataDirectory()
    {
        var config = new ServerConfig();
        config.ParametersDirectory.Should().StartWith(config.DataDirectory);
        config.ParametersDirectory.Should().EndWith("parameters");
    }

    [Fact]
    public void DatabaseDirectory_Default_NestedUnderDataDirectory()
    {
        var config = new ServerConfig();
        config.DatabaseDirectory.Should().StartWith(config.DataDirectory);
        config.DatabaseDirectory.Should().EndWith("database");
    }

    [Fact]
    public void SubDirectories_WhenDataDirectorySet_CascadeFromIt()
    {
        var config = new ServerConfig { DataDirectory = "/custom/data" };

        config.ConfigurationsDirectory.Should().StartWith("/custom/data");
        config.ParametersDirectory.Should().StartWith("/custom/data");
        config.DatabaseDirectory.Should().StartWith("/custom/data");
    }

    [Fact]
    public void ConfigurationsDirectory_CanBeOverriddenIndependently()
    {
        var config = new ServerConfig
        {
            DataDirectory = "/custom/data",
            ConfigurationsDirectory = "/overridden/configs"
        };

        config.ConfigurationsDirectory.Should().Be("/overridden/configs");
        config.ParametersDirectory.Should().StartWith("/custom/data");
        config.DatabaseDirectory.Should().StartWith("/custom/data");
    }

    [Fact]
    public void ParametersDirectory_CanBeOverriddenIndependently()
    {
        var config = new ServerConfig
        {
            DataDirectory = "/custom/data",
            ParametersDirectory = "/overridden/params"
        };

        config.ParametersDirectory.Should().Be("/overridden/params");
        config.ConfigurationsDirectory.Should().StartWith("/custom/data");
        config.DatabaseDirectory.Should().StartWith("/custom/data");
    }

    [Fact]
    public void DatabaseDirectory_CanBeOverriddenIndependently()
    {
        var config = new ServerConfig
        {
            DataDirectory = "/custom/data",
            DatabaseDirectory = "/overridden/db"
        };

        config.DatabaseDirectory.Should().Be("/overridden/db");
        config.ConfigurationsDirectory.Should().StartWith("/custom/data");
        config.ParametersDirectory.Should().StartWith("/custom/data");
    }

    [Fact]
    public void DataDirectory_WhenSetToEmpty_FallsBackToDefault()
    {
        var config = new ServerConfig { DataDirectory = "/custom/data" };
        config.DataDirectory = "";

        config.DataDirectory.Should().NotBeNullOrEmpty();
        config.DataDirectory.Should().NotBe("/custom/data");
    }

    [Fact]
    public void ConfigurationsDirectory_WhenSetToEmpty_FallsBackToDefault()
    {
        var config = new ServerConfig { DataDirectory = "/custom/data", ConfigurationsDirectory = "/overridden/configs" };
        config.ConfigurationsDirectory = "";

        config.ConfigurationsDirectory.Should().StartWith("/custom/data");
    }

    [Fact]
    public void ParametersDirectory_WhenSetToEmpty_FallsBackToDefault()
    {
        var config = new ServerConfig { DataDirectory = "/custom/data", ParametersDirectory = "/overridden/params" };
        config.ParametersDirectory = "";

        config.ParametersDirectory.Should().StartWith("/custom/data");
    }

    [Fact]
    public void DatabaseDirectory_WhenSetToEmpty_FallsBackToDefault()
    {
        var config = new ServerConfig { DataDirectory = "/custom/data", DatabaseDirectory = "/overridden/db" };
        config.DatabaseDirectory = "";

        config.DatabaseDirectory.Should().StartWith("/custom/data");
    }
}
