// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;

using AwesomeAssertions;

using OpenDsc.Client.Commands.Node;

using Xunit;

namespace OpenDsc.Client.Commands.Tests;

[Trait("Category", "Unit")]
[Collection("Session")]
public sealed class NodeCmdletTests : IDisposable
{
    public void Dispose()
    {
        DscClientSession.ClearDefault(null);
    }

    [Fact]
    public void GetDscServerNodeGetNodes_WhenNoDefaultSession_ThrowsTerminatingError()
    {
        using var runspace = CreateRunspace();
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand("Get-DscServerNodeGetNodes");

        var ex = Assert.Throws<CmdletInvocationException>(() => ps.Invoke());

        ex.ErrorRecord.FullyQualifiedErrorId.Should().Contain("OpenDscClient.NoSession");
    }

    [Fact]
    public void GetDscServerNodeGetNodes_AllFilterParametersAreOptional()
    {
        var filterParams = new[] { "FqdnContains", "ConfigurationContains", "Status", "LcmStatus", "Limit" };

        foreach (var paramName in filterParams)
        {
            var prop = typeof(GetDscServerNodeGetNodesCommand).GetProperty(paramName);
            prop.Should().NotBeNull(because: $"parameter {paramName} should exist");

            var paramAttr = prop!.GetCustomAttributes(typeof(ParameterAttribute), inherit: false)
                .Cast<ParameterAttribute>()
                .FirstOrDefault();
            paramAttr.Should().NotBeNull(because: $"{paramName} should have [Parameter] attribute");
            paramAttr!.Mandatory.Should().BeFalse(because: $"{paramName} should be optional");
        }
    }

    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.Create();
        iss.Commands.Add(new SessionStateCmdletEntry("Get-DscServerNodeGetNodes", typeof(GetDscServerNodeGetNodesCommand), null));
        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();
        return runspace;
    }
}
