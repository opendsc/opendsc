// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Schema.Tests;

public class DscRestartRequirementTests
{
    [Fact]
    public void DscRestartRequirement_SystemRestart_ShouldStoreSystemName()
    {
        var requirement = new DscRestartRequirement { System = "SERVER01" };

        requirement.System.Should().Be("SERVER01");
        requirement.Service.Should().BeNull();
        requirement.Process.Should().BeNull();
    }

    [Fact]
    public void DscRestartRequirement_ServiceRestart_ShouldStoreServiceName()
    {
        var requirement = new DscRestartRequirement { Service = "MyService" };

        requirement.Service.Should().Be("MyService");
        requirement.System.Should().BeNull();
        requirement.Process.Should().BeNull();
    }

    [Fact]
    public void DscRestartRequirement_ProcessRestart_ShouldStoreProcessInfo()
    {
        var processInfo = new DscProcessRestartInfo { Name = "app.exe", Id = 1234 };
        var requirement = new DscRestartRequirement { Process = processInfo };

        requirement.Process.Should().NotBeNull();
        requirement.Process!.Name.Should().Be("app.exe");
        requirement.Process.Id.Should().Be(1234u);
        requirement.System.Should().BeNull();
        requirement.Service.Should().BeNull();
    }
}
