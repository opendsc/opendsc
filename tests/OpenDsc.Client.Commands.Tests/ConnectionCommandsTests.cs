// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;

using AwesomeAssertions;

using OpenDsc.Client.Commands;

using Xunit;

namespace OpenDsc.Client.Commands.Tests;

[Trait("Category", "Unit")]
public sealed class ConnectionCommandsTests : IDisposable
{
    public void Dispose()
    {
        DscClientSession.Disconnect();
    }

    [Fact]
    public void ConnectDscServer_ReturnsSessionState()
    {
        using var runspace = CreateRunspace();

        var results = Invoke(runspace, "Connect-DscServer", new Dictionary<string, object?>
        {
            ["ServerUri"] = new Uri("https://server.test/"),
            ["Token"] = "pat_test",
        });

        results.Should().ContainSingle();
        var state = results[0].BaseObject.Should().BeOfType<DscClientSessionState>().Subject;
        state.ServerUri.Should().Be(new Uri("https://server.test/"));
        state.ConnectedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void DisconnectDscServer_ReturnsTrueWhenConnected_ThenFalse()
    {
        using var runspace = CreateRunspace();

        _ = Invoke(runspace, "Connect-DscServer", new Dictionary<string, object?>
        {
            ["ServerUri"] = new Uri("https://server.test/"),
            ["Token"] = "pat_test",
        });

        var first = Invoke(runspace, "Disconnect-DscServer");
        var second = Invoke(runspace, "Disconnect-DscServer");

        first.Should().ContainSingle();
        first[0].BaseObject.Should().Be(true);

        second.Should().ContainSingle();
        second[0].BaseObject.Should().Be(false);
    }

    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.Create();
        iss.Commands.Add(new SessionStateCmdletEntry("Connect-DscServer", typeof(ConnectDscServerCommand), null));
        iss.Commands.Add(new SessionStateCmdletEntry("Disconnect-DscServer", typeof(DisconnectDscServerCommand), null));
        var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();
        return runspace;
    }

    private static IReadOnlyList<PSObject> Invoke(
        Runspace runspace,
        string command,
        IReadOnlyDictionary<string, object?>? parameters = null)
    {
        using var ps = PowerShell.Create();
        ps.Runspace = runspace;
        ps.AddCommand(command);

        if (parameters is not null)
        {
            foreach (var item in parameters)
            {
                ps.AddParameter(item.Key, item.Value);
            }
        }

        var results = ps.Invoke();
        ps.Streams.Error.Should().BeEmpty();
        return results;
    }
}
