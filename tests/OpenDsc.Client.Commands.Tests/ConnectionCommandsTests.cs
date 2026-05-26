// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Security;

using AwesomeAssertions;

using OpenDsc.Client.Commands;

using Xunit;

namespace OpenDsc.Client.Commands.Tests;

[Trait("Category", "Unit")]
[Collection("Session")]
public sealed class ConnectionCommandsTests : IDisposable
{
    public void Dispose()
    {
        DscClientSession.ClearDefault(null);
    }

    [Fact]
    public void NewDscServerSession_ReturnsSession()
    {
        using var runspace = CreateRunspace();

        var token = new SecureString();
        foreach (char c in "pat_test") token.AppendChar(c);
        token.MakeReadOnly();

        var results = Invoke(runspace, "New-DscServerSession", new Dictionary<string, object?>
        {
            ["ServerUri"] = new Uri("https://server.test/"),
            ["Token"] = token,
        });

        results.Should().ContainSingle();
        var session = results[0].BaseObject.Should().BeOfType<DscServerSession>().Subject;
        session.ServerUri.Should().Be(new Uri("https://server.test/"));
        session.ConnectedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void RemoveDscServerSession_ReturnsTrueWhenDefault_ThenFalse()
    {
        using var runspace = CreateRunspace();

        var token = new SecureString();
        foreach (char c in "pat_test") token.AppendChar(c);
        token.MakeReadOnly();

        _ = Invoke(runspace, "New-DscServerSession", new Dictionary<string, object?>
        {
            ["ServerUri"] = new Uri("https://server.test/"),
            ["Token"] = token,
        });

        var first = Invoke(runspace, "Remove-DscServerSession");
        var second = Invoke(runspace, "Remove-DscServerSession");

        first.Should().ContainSingle();
        first[0].BaseObject.Should().Be(true);

        second.Should().ContainSingle();
        second[0].BaseObject.Should().Be(false);
    }

    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.Create();
        iss.Commands.Add(new SessionStateCmdletEntry("New-DscServerSession", typeof(NewDscServerSessionCommand), null));
        iss.Commands.Add(new SessionStateCmdletEntry("Remove-DscServerSession", typeof(RemoveDscServerSessionCommand), null));
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
