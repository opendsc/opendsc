// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using Xunit;

using OpenDsc.Client.Services;
using OpenDsc.Client.Tests.Helpers;
using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Reports;

namespace OpenDsc.Client.Tests.Services;

public sealed class NodeHttpServiceTests
{
    private static NodeHttpService CreateService(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new NodeHttpService(client);
    }

    // ── GetNodesAsync ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodesAsync_Gets_Nodes_Endpoint()
    {
        var nodes = new List<NodeSummary> { new() { Fqdn = "server1.corp" } };
        var handler = new FakeHttpMessageHandler().RespondOk(nodes);
        var service = CreateService(handler);

        var result = await service.GetNodesAsync(cancellationToken: TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/nodes");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodesAsync_With_Filter_Builds_Query_String()
    {
        var nodes = new List<NodeSummary>();
        var handler = new FakeHttpMessageHandler().RespondOk(nodes);
        var service = CreateService(handler);

        var filter = new NodeFilterRequest { FqdnContains = "corp", Limit = 10 };
        await service.GetNodesAsync(filter, TestContext.Current.CancellationToken);

        var url = handler.LastRequest!.RequestUri!.ToString();
        url.Should().Contain("fqdnContains=corp");
        url.Should().Contain("limit=10");
    }

    // ── GetNodeAsync ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodeAsync_Gets_Node_By_Id()
    {
        var nodeId = Guid.NewGuid();
        var node = new NodeDetails { Summary = new NodeSummary(), Id = nodeId, Fqdn = "server1.corp" };
        var handler = new FakeHttpMessageHandler().RespondOk(node);
        var service = CreateService(handler);

        var result = await service.GetNodeAsync(nodeId, TestContext.Current.CancellationToken);

        result!.Id.Should().Be(nodeId);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodeAsync_Returns_Null_On_404()
    {
        var nodeId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var result = await service.GetNodeAsync(nodeId, TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ── DeleteNodeAsync ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteNodeAsync_Deletes_Node()
    {
        var nodeId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.DeleteNodeAsync(nodeId, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}");
    }

    // ── AssignConfigurationAsync ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task AssignConfigurationAsync_Puts_Configuration()
    {
        var nodeId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.AssignConfigurationAsync(nodeId, new AssignConfigurationRequest { ConfigurationName = "web" }, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/configuration");
    }

    // ── RemoveConfigurationAsync ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RemoveConfigurationAsync_Deletes_Configuration()
    {
        var nodeId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.RemoveConfigurationAsync(nodeId, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/configuration");
    }

    // ── GetNodeReportsAsync ───────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodeReportsAsync_Gets_Reports_Endpoint()
    {
        var nodeId = Guid.NewGuid();
        var reports = new List<ReportSummary> { new() { Id = Guid.NewGuid() } };
        var handler = new FakeHttpMessageHandler().RespondOk(reports);
        var service = CreateService(handler);

        var result = await service.GetNodeReportsAsync(nodeId, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/reports");
    }

    // ── GetNodeStatusEventsAsync ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodeStatusEventsAsync_Gets_Status_History_Endpoint()
    {
        var nodeId = Guid.NewGuid();
        var events = new List<NodeStatusEventSummary> { new() };
        var handler = new FakeHttpMessageHandler().RespondOk(events);
        var service = CreateService(handler);

        var result = await service.GetNodeStatusEventsAsync(nodeId, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/status-history");
    }

    // ── GetNodeTagsAsync ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodeTagsAsync_Gets_Tags_Endpoint()
    {
        var nodeId = Guid.NewGuid();
        var tags = new List<NodeTagSummary> { new() };
        var handler = new FakeHttpMessageHandler().RespondOk(tags);
        var service = CreateService(handler);

        var result = await service.GetNodeTagsAsync(nodeId, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/tags");
    }

    // ── AddNodeTagAsync ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task AddNodeTagAsync_Posts_To_Tags_Endpoint()
    {
        var nodeId = Guid.NewGuid();
        var created = new NodeTagSummary { ScopeTypeId = Guid.NewGuid() };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);

        var result = await service.AddNodeTagAsync(nodeId, new AddNodeTagRequest(), TestContext.Current.CancellationToken);

        result.ScopeTypeId.Should().Be(created.ScopeTypeId);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/tags");
    }

    // ── RemoveNodeTagAsync ────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RemoveNodeTagAsync_Deletes_Tag_By_ScopeValueId()
    {
        var nodeId = Guid.NewGuid();
        var scopeValueId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.RemoveNodeTagAsync(nodeId, new RemoveNodeTagRequest { ScopeValueId = scopeValueId }, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/tags/{scopeValueId}");
    }

    // ── GetScopeTypesAsync ────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetScopeTypesAsync_Gets_Scope_Types_Endpoint()
    {
        var types = new List<ScopeTypeSummary> { new() { Name = "env" } };
        var handler = new FakeHttpMessageHandler().RespondOk(types);
        var service = CreateService(handler);

        var result = await service.GetScopeTypesAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/scope-types");
    }

    // ── Error mapping ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteNodeAsync_Throws_KeyNotFoundException_On_404()
    {
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var act = async () => await service.DeleteNodeAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Configuration and scope endpoints ────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodeAssignmentAsync_Gets_Assignment_Endpoint()
    {
        var nodeId = Guid.NewGuid();
        var assignment = new NodeAssignmentSummary { NodeId = nodeId, ConfigurationName = "web" };
        var handler = new FakeHttpMessageHandler().RespondOk(assignment);
        var service = CreateService(handler);

        var result = await service.GetNodeAssignmentAsync(nodeId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/assignment");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAvailableConfigurationsAsync_Gets_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondOk(new List<ConfigurationOption> { new() { Name = "web" } });
        var service = CreateService(handler);

        var result = await service.GetAvailableConfigurationsAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/nodes/available-configurations");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAvailableCompositeConfigurationsAsync_Gets_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondOk(new List<ConfigurationOption> { new() { Name = "composite" } });
        var service = CreateService(handler);

        var result = await service.GetAvailableCompositeConfigurationsAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/nodes/available-composite-configurations");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAssignableConfigurationsAsync_Gets_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondOk(new List<ConfigurationAssignmentOption> { new() { Name = "web", MajorVersions = [1] } });
        var service = CreateService(handler);

        var result = await service.GetAssignableConfigurationsAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/nodes/assignable-configurations");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAssignableCompositeConfigurationsAsync_Gets_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondOk(new List<ConfigurationAssignmentOption> { new() { Name = "composite", MajorVersions = [1] } });
        var service = CreateService(handler);

        var result = await service.GetAssignableCompositeConfigurationsAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/nodes/assignable-composite-configurations");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodeScopeValuesAsync_Gets_Endpoint()
    {
        var nodeId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondOk(new List<NodeScopeValueSummary> { new() });
        var service = CreateService(handler);

        var result = await service.GetNodeScopeValuesAsync(nodeId, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/scope-values");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetScopeValuesAsync_Gets_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondOk(new List<ScopeValueSummary> { new() });
        var service = CreateService(handler);

        var result = await service.GetScopeValuesAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/scope-values");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodeConfigurationManifestAsync_Gets_Manifest_Endpoint()
    {
        var nodeId = Guid.NewGuid();
        var manifest = new NodeConfigurationManifest { EntryPoint = "main.dsc.yaml", Content = "resources: []" };
        var handler = new FakeHttpMessageHandler().RespondOk(manifest);
        var service = CreateService(handler);

        var result = await service.GetNodeConfigurationManifestAsync(nodeId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.EntryPoint.Should().Be("main.dsc.yaml");
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/configuration/manifest");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodeConfigurationBundleAsync_Gets_Bundle_Download_Endpoint()
    {
        var nodeId = Guid.NewGuid();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[] { 1, 2, 3 })
        };
        response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
        response.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
        {
            FileName = "node-config.zip"
        };
        var handler = new FakeHttpMessageHandler().Respond(response);
        var service = CreateService(handler);

        var result = await service.GetNodeConfigurationBundleAsync(nodeId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Content.Should().HaveCount(3);
        result.FileName.Should().Contain("node-config.zip");
        result.ContentType.Should().Be("application/zip");
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/configuration/bundle/download");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetConfigurationChecksumAsync_Gets_Endpoint()
    {
        var nodeId = Guid.NewGuid();
        var checksum = new ConfigurationChecksumResponse { Checksum = "abc", EntryPoint = "main.dsc.yaml" };
        var handler = new FakeHttpMessageHandler().RespondOk(checksum);
        var service = CreateService(handler);

        var result = await service.GetConfigurationChecksumAsync(nodeId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/configuration/checksum");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CheckConfigurationChangedAsync_Returns_False_When_Checksum_Matches()
    {
        var nodeId = Guid.NewGuid();
        var checksum = new ConfigurationChecksumResponse { Checksum = "abc", EntryPoint = "main.dsc.yaml" };
        var handler = new FakeHttpMessageHandler().RespondOk(checksum);
        var service = CreateService(handler);

        var result = await service.CheckConfigurationChangedAsync(nodeId, "abc", TestContext.Current.CancellationToken);

        result.Should().BeFalse();
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/configuration/checksum");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SetNodeScopeValueAsync_Puts_To_Endpoint()
    {
        var nodeId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.SetNodeScopeValueAsync(nodeId, new SetNodeScopeValueRequest { ScopeTypeId = Guid.NewGuid(), ScopeValue = "east" }, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/scope-values");
    }
}
