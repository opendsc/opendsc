// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;

using AwesomeAssertions;

using OpenDsc.Client.Commands;
using OpenDsc.Client.Commands.Node;
using OpenDsc.Contracts.Nodes;

using Xunit;

namespace OpenDsc.Client.Commands.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class NodeCmdletIntegrationTests : IAsyncLifetime
{
    private readonly LightweightServer _server = new();

    public async ValueTask InitializeAsync()
    {
        await _server.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _server.DisposeAsync();
        DscClientSession.ClearDefault(null);
    }

    [Fact]
    public void GetDscServerNodeGetNodes_ReturnsNodeSummaries()
    {
        using var runspace = CreateRunspace();

        CreateSession(runspace);
        var results = Invoke(runspace, "Get-DscServerNodeGetNodes");

        results.Should().ContainSingle();
        var node = results[0].BaseObject.Should().BeOfType<NodeSummary>().Subject;
        node.Fqdn.Should().Be("server1.corp");
    }

    [Fact]
    public void GetDscServerNodeGetNodes_WithFqdnContains_AppendsQueryParam()
    {
        using var runspace = CreateRunspace();

        CreateSession(runspace);
        Invoke(runspace, "Get-DscServerNodeGetNodes", new Dictionary<string, object?>
        {
            ["FqdnContains"] = "corp",
        });

        _server.LastRequestUrl.Should().Contain("fqdnContains=corp");
    }

    [Fact]
    public void GetDscServerNodeGetNodes_WithLimit_AppendsQueryParam()
    {
        using var runspace = CreateRunspace();

        CreateSession(runspace);
        Invoke(runspace, "Get-DscServerNodeGetNodes", new Dictionary<string, object?>
        {
            ["Limit"] = 5,
        });

        _server.LastRequestUrl.Should().Contain("limit=5");
    }

    [Fact]
    public void GetDscServerNodeGetNodes_WithMultipleFilters_AppendsAllQueryParams()
    {
        using var runspace = CreateRunspace();

        CreateSession(runspace);
        Invoke(runspace, "Get-DscServerNodeGetNodes", new Dictionary<string, object?>
        {
            ["FqdnContains"] = "corp",
            ["Limit"] = 10,
        });

        _server.LastRequestUrl.Should().Contain("fqdnContains=corp");
        _server.LastRequestUrl.Should().Contain("limit=10");
    }

    [Fact]
    public void GetDscServerNodeGetNodes_WithNoFilters_CallsBaseEndpoint()
    {
        using var runspace = CreateRunspace();

        CreateSession(runspace);
        Invoke(runspace, "Get-DscServerNodeGetNodes");

        _server.LastRequestUrl.Should().Contain("api/v1/nodes");
    }

    private void CreateSession(Runspace runspace)
    {
        _ = Invoke(runspace, "New-DscServerSession", new Dictionary<string, object?>
        {
            ["ServerUri"] = _server.BaseAddress,
            ["Token"] = CreateToken("pat_test"),
        });
    }

    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.Create();
        iss.Commands.Add(new SessionStateCmdletEntry("New-DscServerSession", typeof(NewDscServerSessionCommand), null));
        iss.Commands.Add(new SessionStateCmdletEntry("Remove-DscServerSession", typeof(RemoveDscServerSessionCommand), null));
        iss.Commands.Add(new SessionStateCmdletEntry("Get-DscServerNodeGetNodes", typeof(GetDscServerNodeGetNodesCommand), null));
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

    private static SecureString CreateToken(string value)
    {
        var s = new SecureString();
        foreach (char c in value) s.AppendChar(c);
        s.MakeReadOnly();
        return s;
    }

    private sealed class LightweightServer : IAsyncDisposable
    {
        private readonly HttpListener _listener = new();
        private CancellationTokenSource? _cts;
        private Task? _worker;

        public Uri BaseAddress { get; private set; } = null!;

        public string? LastRequestUrl { get; private set; }

        public Task StartAsync()
        {
            var port = GetOpenPort();
            BaseAddress = new Uri($"http://127.0.0.1:{port}/");
            _listener.Prefixes.Add(BaseAddress.ToString());
            _listener.Start();

            _cts = new CancellationTokenSource();
            _worker = Task.Run(() => ProcessRequestsAsync(_cts.Token));

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            if (_cts is null)
            {
                return;
            }

            _cts.Cancel();
            _listener.Stop();

            if (_worker is not null)
            {
                try
                {
                    await _worker;
                }
                catch (OperationCanceledException)
                {
                }
                catch (HttpListenerException)
                {
                }
            }

            _listener.Close();
            _cts.Dispose();
        }

        private async Task ProcessRequestsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var context = await _listener.GetContextAsync();
                LastRequestUrl = context.Request.Url?.ToString();
                var path = context.Request.Url?.AbsolutePath?.Trim('/') ?? string.Empty;

                if (string.Equals(path, "api/v1/nodes", StringComparison.OrdinalIgnoreCase))
                {
                    const string json = "[{\"id\":\"f47ac10b-58cc-4372-a567-0e02b2c3d479\",\"fqdn\":\"server1.corp\",\"status\":\"Compliant\",\"lcmStatus\":\"Idle\",\"isStale\":false,\"createdAt\":\"2026-01-01T00:00:00Z\",\"configurationSource\":\"Pull\"}]";
                    var bytes = Encoding.UTF8.GetBytes(json);
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes, cancellationToken);
                    context.Response.Close();
                    continue;
                }

                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
            }
        }

        private static int GetOpenPort()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            return ((IPEndPoint)listener.LocalEndpoint).Port;
        }
    }
}
