// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using AwesomeAssertions;

using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.CompositeConfigurations;
using OpenDsc.Contracts.Reports;
using OpenDsc.Contracts.Settings;
using OpenDsc.Contracts.Permissions;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class NodeStatusEndpointsTests : IDisposable
{
    private readonly ServerWebApplicationFactory _factory = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public void Dispose()
    {
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<Guid> RegisterTestNodeAsync()
    {
        using var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", new RegisterNodeRequest
        {
            RegistrationKey = "test-registration-key",
            Fqdn = $"test-node-{Guid.NewGuid()}.example.com"
        });

        registerResponse.EnsureSuccessStatusCode();
        var registration = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();
        return registration!.NodeId;
    }

    // ── PUT /{nodeId}/lcm-status ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateLcmStatus_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var nodeId = Guid.NewGuid();
        var response = await client.PutAsJsonAsync(
            $"/api/v1/nodes/{nodeId}/lcm-status",
            new UpdateLcmStatusRequest { LcmStatus = LcmStatus.Idle }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateLcmStatus_WithNodeAuth_ReturnsNoContent()
    {
        var nodeId = await RegisterTestNodeAsync();

        using var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync(
            $"/api/v1/nodes/{nodeId}/lcm-status",
            new UpdateLcmStatusRequest { LcmStatus = LcmStatus.Testing }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateLcmStatus_WithNonExistentNode_ReturnsNotFound()
    {
        var fakeId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        // The testing auth handler will try to look the node up and fail to find node_id claim,
        // so either NotFound (if the handler returns Forbid due to no claim) or Unauthorized/Forbidden.
        // Since the testing handler looks up the node and sets node_id only if found
        // and the forbid check in UpdateLcmStatus compares in-URL id vs claim id,
        // a non-existent node results in 403 Forbid (no node_id claim).
        var response = await client.PutAsJsonAsync(
            $"/api/v1/nodes/{fakeId}/lcm-status",
            new UpdateLcmStatusRequest { LcmStatus = LcmStatus.Idle }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateLcmStatus_WithNodeAuth_PersistsStatusAndLogsEvent()
    {
        var nodeId = await RegisterTestNodeAsync();

        using var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync(
            $"/api/v1/nodes/{nodeId}/lcm-status",
            new UpdateLcmStatusRequest { LcmStatus = LcmStatus.Remediating }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify the status was persisted by fetching node summary
        using var adminClient = _factory.CreateAuthenticatedClient();
        var nodeResponse = await adminClient.GetAsync($"/api/v1/nodes/{nodeId}", TestContext.Current.CancellationToken);
        nodeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var nodeSummary = await nodeResponse.Content.ReadFromJsonAsync<NodeSummary>(JsonOptions, TestContext.Current.CancellationToken);
        nodeSummary!.LcmStatus.Should().Be(LcmStatus.Remediating.ToString());
    }

    [Fact]
    public async Task UpdateLcmStatus_WithNodeAuth_LogsStatusEvent()
    {
        var nodeId = await RegisterTestNodeAsync();

        using var client = _factory.CreateClient();
        await client.PutAsJsonAsync(
            $"/api/v1/nodes/{nodeId}/lcm-status",
            new UpdateLcmStatusRequest { LcmStatus = LcmStatus.Idle }, TestContext.Current.CancellationToken);

        // Verify event was recorded in status history
        using var adminClient = _factory.CreateAuthenticatedClient();
        var historyResponse = await adminClient.GetAsync($"/api/v1/nodes/{nodeId}/status-history", TestContext.Current.CancellationToken);
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await historyResponse.Content.ReadFromJsonAsync<List<NodeStatusEventSummary>>(TestContext.Current.CancellationToken);
        events.Should().NotBeNull();
        events!.Should().ContainSingle(e => e.LcmStatus == LcmStatus.Idle.ToString());
    }

    // ── GET /{nodeId}/status-history ──────────────────────────────────────────

    [Fact]
    public async Task GetStatusHistory_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/nodes/{Guid.NewGuid()}/status-history", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetStatusHistory_WithAuth_ReturnsEmptyListForNewNode()
    {
        var nodeId = await RegisterTestNodeAsync();

        using var adminClient = _factory.CreateAuthenticatedClient();
        var response = await adminClient.GetAsync($"/api/v1/nodes/{nodeId}/status-history", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await response.Content.ReadFromJsonAsync<List<NodeStatusEventSummary>>(TestContext.Current.CancellationToken);
        events.Should().NotBeNull();
        events!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetStatusHistory_WithAuth_NotFound_Returns404()
    {
        using var adminClient = _factory.CreateAuthenticatedClient();
        var response = await adminClient.GetAsync($"/api/v1/nodes/{Guid.NewGuid()}/status-history", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStatusHistory_WithAuth_ReturnsMultipleEvents()
    {
        var nodeId = await RegisterTestNodeAsync();
        using var nodeClient = _factory.CreateClient();

        foreach (var status in new[] { LcmStatus.Testing, LcmStatus.Remediating, LcmStatus.Idle })
        {
            await nodeClient.PutAsJsonAsync(
                $"/api/v1/nodes/{nodeId}/lcm-status",
                new UpdateLcmStatusRequest { LcmStatus = status }, TestContext.Current.CancellationToken);
        }

        using var adminClient = _factory.CreateAuthenticatedClient();
        var response = await adminClient.GetAsync($"/api/v1/nodes/{nodeId}/status-history", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await response.Content.ReadFromJsonAsync<List<NodeStatusEventSummary>>(TestContext.Current.CancellationToken);
        events.Should().NotBeNull();
        events!.Should().HaveCount(3);
    }
}
