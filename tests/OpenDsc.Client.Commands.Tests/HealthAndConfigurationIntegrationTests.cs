// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Net.Sockets;
using System.Text;

using AwesomeAssertions;

using OpenDsc.Client.Commands;
using OpenDsc.Contracts.Configurations;

using Xunit;

namespace OpenDsc.Client.Commands.Tests;

[Trait("Category", "Integration")]
public sealed class HealthAndConfigurationIntegrationTests : IAsyncLifetime
{
    private readonly LightweightServer _server = new();

    public async ValueTask InitializeAsync()
    {
        await _server.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _server.DisposeAsync();
        DscClientSession.Disconnect();
    }

    [Fact]
    public void GeneratedHealthAndConfigurationCmdlets_WorkEndToEnd()
    {
        using var runspace = CreateRunspace();

        _ = Invoke(runspace, "Connect-DscServer", new Dictionary<string, object?>
        {
            ["ServerUri"] = _server.BaseAddress,
            ["Token"] = "pat_test",
        });

        var health = Invoke(runspace, "Test-DscServerHealthConnect");
        var configs = Invoke(runspace, "Get-DscServerConfigurationGetConfigurations");

        health.Should().ContainSingle();
        health[0].BaseObject.Should().Be(true);

        configs.Should().ContainSingle();
        var config = configs[0].BaseObject.Should().BeOfType<ConfigurationSummary>().Subject;
        config.Name.Should().Be("SampleConfig");
        config.LatestVersion.Should().Be("1.0.0");

        _server.LastAuthorizationHeader.Should().Be("Bearer pat_test");
    }

    private static Runspace CreateRunspace()
    {
        var iss = InitialSessionState.Create();
        iss.Commands.Add(new SessionStateCmdletEntry("Connect-DscServer", typeof(ConnectDscServerCommand), null));
        iss.Commands.Add(new SessionStateCmdletEntry("Disconnect-DscServer", typeof(DisconnectDscServerCommand), null));
        iss.Commands.Add(new SessionStateCmdletEntry("Test-DscServerHealthConnect", typeof(TestDscServerHealthConnectCommand), null));
        iss.Commands.Add(new SessionStateCmdletEntry("Get-DscServerConfigurationGetConfigurations", typeof(GetDscServerConfigurationGetConfigurationsCommand), null));

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

    private sealed class LightweightServer : IAsyncDisposable
    {
        private readonly HttpListener _listener = new();
        private CancellationTokenSource? _cts;
        private Task? _worker;

        public Uri BaseAddress { get; private set; } = null!;

        public string? LastAuthorizationHeader { get; private set; }

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
                LastAuthorizationHeader = context.Request.Headers["Authorization"];
                var path = context.Request.Url?.AbsolutePath?.Trim('/') ?? string.Empty;

                if (string.Equals(path, "health/ready", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    context.Response.Close();
                    continue;
                }

                if (string.Equals(path, "api/v1/configurations", StringComparison.OrdinalIgnoreCase))
                {
                    const string json = "[{\"id\":\"0f6be6f3-9e77-4d74-a1d0-5b869dc8313d\",\"name\":\"SampleConfig\",\"description\":\"Example\",\"useServerManagedParameters\":true,\"versionCount\":1,\"latestVersion\":\"1.0.0\",\"hasPublishedVersion\":true,\"createdAt\":\"2026-01-01T00:00:00Z\"}]";
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
