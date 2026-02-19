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
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNodeReports_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var nodeId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/v1/nodes/{nodeId}/reports");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNodeReports_WithAuth_ReturnsReports()
    {
        var nodeId = await RegisterTestNodeAsync();

        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/nodes/{nodeId}/reports");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reports = await response.Content.ReadFromJsonAsync<List<ReportSummary>>();
        reports.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAllReports_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/reports");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllReports_WithAuth_ReturnsAllReports()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/reports");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reports = await response.Content.ReadFromJsonAsync<List<ReportSummary>>();
        reports.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReport_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var reportId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/v1/reports/{reportId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReport_NotFound_ReturnsNotFound()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync($"/api/v1/reports/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetReport_AsAdmin_ReturnsNotFound()
    {
        // Note: This test changed - nodes now authenticate via mTLS, not API keys
        // Testing admin report retrieval for non-existent report
        using var adminClient = _factory.CreateAuthenticatedClient("test-admin-key");
        var getResponse = await adminClient.GetAsync($"/api/v1/reports/{Guid.NewGuid()}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetNodeReports_WithoutSubmit_ReturnsEmptyList()
    {
        // Note: Nodes now use mTLS for authentication, this tests admin access
        var nodeId = await RegisterTestNodeAsync();

        using var adminClient = _factory.CreateAuthenticatedClient();
        var getResponse = await adminClient.GetAsync($"/api/v1/nodes/{nodeId}/reports");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reports = await getResponse.Content.ReadFromJsonAsync<List<ReportSummary>>();
        reports.Should().NotBeNull();
        reports!.Should().BeEmpty();
    }
}
