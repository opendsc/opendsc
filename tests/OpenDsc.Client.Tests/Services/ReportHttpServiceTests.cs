// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using Xunit;

using OpenDsc.Client.Services;
using OpenDsc.Client.Tests.Helpers;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Reports;

namespace OpenDsc.Client.Tests.Services;

public sealed class ReportHttpServiceTests
{
    private static ReportHttpService CreateService(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new ReportHttpService(client);
    }

    // ── GetReportsAsync ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetReportsAsync_Gets_Reports_Endpoint()
    {
        var reports = new List<ReportSummary> { new() { Id = Guid.NewGuid() } };
        var handler = new FakeHttpMessageHandler().RespondOk(reports);
        var service = CreateService(handler);

        var result = await service.GetReportsAsync(cancellationToken: TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/reports");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetReportsAsync_With_NodeId_Appends_QueryString()
    {
        var nodeId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondOk(new List<ReportSummary>());
        var service = CreateService(handler);

        await service.GetReportsAsync(nodeId: nodeId, cancellationToken: TestContext.Current.CancellationToken);

        handler.LastRequest!.RequestUri!.ToString().Should().Contain($"nodeId={nodeId}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetReportsAsync_With_Pagination_Appends_QueryString()
    {
        var from = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var handler = new FakeHttpMessageHandler().RespondOk(new List<ReportSummary>());
        var service = CreateService(handler);

        await service.GetReportsAsync(skip: 10, take: 20, from: from, cancellationToken: TestContext.Current.CancellationToken);

        var url = handler.LastRequest!.RequestUri!.ToString();
        url.Should().Contain("skip=10");
        url.Should().Contain("take=20");
        url.Should().Contain("from=");
    }

    // ── GetReportAsync ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetReportAsync_Gets_Report_By_Id()
    {
        var reportId = Guid.NewGuid();
        var report = new ReportDetails { Id = reportId };
        var handler = new FakeHttpMessageHandler().RespondOk(report);
        var service = CreateService(handler);

        var result = await service.GetReportAsync(reportId, TestContext.Current.CancellationToken);

        result!.Id.Should().Be(reportId);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/reports/{reportId}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetReportAsync_Returns_Null_On_404()
    {
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var result = await service.GetReportAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ── SubmitReportAsync ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SubmitReportAsync_Posts_To_Node_Reports_Endpoint()
    {
        var nodeId = Guid.NewGuid();
        var reportId = Guid.NewGuid();
        var summary = new ReportSummary { Id = reportId };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, summary);
        var service = CreateService(handler);

        var result = await service.SubmitReportAsync(nodeId, new SubmitReportRequest(), TestContext.Current.CancellationToken);

        result.Id.Should().Be(reportId);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/nodes/{nodeId}/reports");
    }

    // ── Additional endpoints ──────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetReportNodeAsync_Gets_Report_Node_Endpoint()
    {
        var reportId = Guid.NewGuid();
        var node = new NodeSummary { Id = Guid.NewGuid(), Fqdn = "node1.contoso.com" };
        var handler = new FakeHttpMessageHandler().RespondOk(node);
        var service = CreateService(handler);

        var result = await service.GetReportNodeAsync(reportId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Fqdn.Should().Be("node1.contoso.com");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/reports/{reportId}/node");
    }
}
