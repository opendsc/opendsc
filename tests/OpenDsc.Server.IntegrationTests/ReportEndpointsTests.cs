// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Schema;
using OpenDsc.Server.Contracts;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class ReportEndpointsTests : IDisposable
{
    private readonly ServerWebApplicationFactory _factory = new();

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

        var registration = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();
        return registration!.NodeId;
    }

    [Fact]
    public async Task SubmitReport_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var nodeId = Guid.NewGuid();
        var response = await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/reports", new SubmitReportRequest
        {
            Operation = DscOperation.Test,
            Result = new DscResult { HadErrors = false }
        }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNodeReports_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var nodeId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/v1/nodes/{nodeId}/reports", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNodeReports_WithAuth_ReturnsReports()
    {
        var nodeId = await RegisterTestNodeAsync();

        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/nodes/{nodeId}/reports", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reports = await response.Content.ReadFromJsonAsync<List<ReportSummary>>(TestContext.Current.CancellationToken);
        reports.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllReports_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/reports", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllReports_WithAuth_ReturnsAllReports()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/reports", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reports = await response.Content.ReadFromJsonAsync<List<ReportSummary>>(TestContext.Current.CancellationToken);
        reports.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReport_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var reportId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/v1/reports/{reportId}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReport_NotFound_ReturnsNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/reports/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetReport_AsAdmin_ReturnsNotFound()
    {
        // Note: This test changed - nodes now authenticate via mTLS, not API keys
        // Testing admin report retrieval for non-existent report
        using var adminClient = _factory.CreateAuthenticatedClient("test-admin-key");
        var getResponse = await adminClient.GetAsync($"/api/v1/reports/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetNodeReports_WithoutSubmit_ReturnsEmptyList()
    {
        // Note: Nodes now use mTLS for authentication, this tests admin access
        var nodeId = await RegisterTestNodeAsync();

        using var adminClient = _factory.CreateAuthenticatedClient();
        var getResponse = await adminClient.GetAsync($"/api/v1/nodes/{nodeId}/reports", TestContext.Current.CancellationToken);

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reports = await getResponse.Content.ReadFromJsonAsync<List<ReportSummary>>(TestContext.Current.CancellationToken);
        reports.Should().NotBeNull();
        reports!.Should().BeEmpty();
    }

    [Fact]
    public async Task SubmitReport_DoesNotCreateStatusHistoryEvent()
    {
        var nodeId = await RegisterTestNodeAsync();

        using var nodeClient = _factory.CreateClient();
        var reportResponse = await nodeClient.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/reports", new SubmitReportRequest
        {
            Operation = DscOperation.Test,
            Result = new DscResult { HadErrors = false, Messages = [], Results = [] }
        }, TestContext.Current.CancellationToken);

        reportResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Compliance reports no longer create status history events — verify the history is empty
        using var adminClient = _factory.CreateAuthenticatedClient();
        var historyResponse = await adminClient.GetAsync($"/api/v1/nodes/{nodeId}/status-history", TestContext.Current.CancellationToken);
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var events = await historyResponse.Content.ReadFromJsonAsync<List<NodeStatusEventSummary>>(TestContext.Current.CancellationToken);
        events.Should().NotBeNull();
        events!.Should().BeEmpty();
    }
}
