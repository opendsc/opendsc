// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Headers;

using FluentAssertions;

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

    private HttpClient CreateAuthenticatedClient(string apiKey)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return client;
    }

    private async Task<(Guid NodeId, string ApiKey)> RegisterTestNodeAsync()
    {
        using var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", new RegisterNodeRequest
        {
            Fqdn = $"test-node-{Guid.NewGuid()}.example.com",
            RegistrationKey = "test-registration-key"
        });

        var registration = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();
        return (registration!.NodeId, registration.ApiKey);
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
    public async Task SubmitReport_ValidTest_ReturnsCreated()
    {
        var (nodeId, apiKey) = await RegisterTestNodeAsync();

        using var client = CreateAuthenticatedClient(apiKey);

        var report = new SubmitReportRequest
        {
            Operation = DscOperation.Test,
            Result = new DscResult
            {
                HadErrors = false,
                Results = []
            }
        };

        var response = await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/reports", report);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task SubmitReport_ValidSet_ReturnsCreated()
    {
        var (nodeId, apiKey) = await RegisterTestNodeAsync();

        using var client = CreateAuthenticatedClient(apiKey);

        var report = new SubmitReportRequest
        {
            Operation = DscOperation.Set,
            Result = new DscResult
            {
                HadErrors = false,
                Results = []
            }
        };

        var response = await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/reports", report);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
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
        var (nodeId, _) = await RegisterTestNodeAsync();

        using var client = CreateAuthenticatedClient("test-admin-key");

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
        using var client = CreateAuthenticatedClient("test-admin-key");

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
        using var client = CreateAuthenticatedClient("test-admin-key");

        var response = await client.GetAsync($"/api/v1/reports/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitReport_AfterCreating_CanRetrieve()
    {
        var (nodeId, apiKey) = await RegisterTestNodeAsync();

        using var client = CreateAuthenticatedClient(apiKey);

        var report = new SubmitReportRequest
        {
            Operation = DscOperation.Test,
            Result = new DscResult
            {
                HadErrors = false,
                Results = []
            }
        };

        var submitResponse = await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/reports", report);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var reportLocation = submitResponse.Headers.Location!.ToString();
        var reportId = Guid.Parse(reportLocation.Split('/').Last());

        using var adminClient = CreateAuthenticatedClient("test-admin-key");
        var getResponse = await adminClient.GetAsync($"/api/v1/reports/{reportId}");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedReport = await getResponse.Content.ReadFromJsonAsync<ReportDetails>();
        retrievedReport.Should().NotBeNull();
        retrievedReport!.NodeId.Should().Be(nodeId);
        retrievedReport.Operation.Should().Be(DscOperation.Test);
    }

    [Fact]
    public async Task GetNodeReports_AfterSubmitting_ReturnsReport()
    {
        var (nodeId, apiKey) = await RegisterTestNodeAsync();

        using var client = CreateAuthenticatedClient(apiKey);

        var report = new SubmitReportRequest
        {
            Operation = DscOperation.Set,
            Result = new DscResult
            {
                HadErrors = true,
                Results = []
            }
        };

        await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/reports", report);

        using var adminClient = CreateAuthenticatedClient("test-admin-key");
        var getResponse = await adminClient.GetAsync($"/api/v1/nodes/{nodeId}/reports");

        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reports = await getResponse.Content.ReadFromJsonAsync<List<ReportSummary>>();
        reports.Should().NotBeNull();
        reports!.Should().Contain(r => r.NodeId == nodeId && r.Operation == DscOperation.Set);
    }
}
