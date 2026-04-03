// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Lcm.Contracts;
using OpenDsc.Schema;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Mcp;

using Xunit;

namespace OpenDsc.Server.Tests.Mcp;

[Trait("Category", "Unit")]
public class ReportToolsTests : IDisposable
{
    private readonly ServerDbContext _db;
    private readonly ReportTools _tools;

    public ReportToolsTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ServerDbContext(options);
        _tools = new ReportTools(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetRecentReports_ReturnsEmpty_WhenNoReports()
    {
        var result = await _tools.GetRecentReports(null, TestContext.Current.CancellationToken);

        result.Should().Contain("No compliance reports found");
    }

    [Fact]
    public async Task GetRecentReports_ReturnsReports()
    {
        var node = CreateNode("web01.opendsc.dev");
        _db.Nodes.Add(node);
        _db.Reports.Add(CreateReport(node, true, false));
        _db.Reports.Add(CreateReport(node, false, false));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetRecentReports(10, TestContext.Current.CancellationToken);

        result.Should().Contain("Recent Reports (2)");
        result.Should().Contain("web01.opendsc.dev");
    }

    [Fact]
    public async Task GetRecentReports_RespectsCount()
    {
        var node = CreateNode("web01.opendsc.dev");
        _db.Nodes.Add(node);
        for (int i = 0; i < 5; i++)
        {
            _db.Reports.Add(CreateReport(node, true, false));
        }
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetRecentReports(2, TestContext.Current.CancellationToken);

        result.Should().Contain("Recent Reports (2)");
    }

    [Fact]
    public async Task GetNodeReports_ReturnsReportsForNode()
    {
        var node = CreateNode("web01.opendsc.dev");
        _db.Nodes.Add(node);
        _db.Reports.Add(CreateReport(node, true, false));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetNodeReports("web01.opendsc.dev", null, TestContext.Current.CancellationToken);

        result.Should().Contain("web01.opendsc.dev");
        result.Should().Contain("showing 1");
    }

    [Fact]
    public async Task GetNodeReports_ReturnsNotFound_WhenNodeDoesNotExist()
    {
        var result = await _tools.GetNodeReports("nonexistent.opendsc.dev", null, TestContext.Current.CancellationToken);

        result.Should().Contain("not found");
    }

    [Fact]
    public async Task GetFailedReports_ReturnsEmpty_WhenAllCompliant()
    {
        var node = CreateNode("web01.opendsc.dev");
        _db.Nodes.Add(node);
        _db.Reports.Add(CreateReport(node, true, false));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetFailedReports(null, TestContext.Current.CancellationToken);

        result.Should().Contain("No failed or non-compliant reports found");
    }

    [Fact]
    public async Task GetFailedReports_ReturnsFailedReports()
    {
        var node = CreateNode("web01.opendsc.dev");
        _db.Nodes.Add(node);
        _db.Reports.Add(CreateReport(node, true, false));
        _db.Reports.Add(CreateReport(node, false, false));
        _db.Reports.Add(CreateReport(node, true, true));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetFailedReports(null, TestContext.Current.CancellationToken);

        result.Should().Contain("Failed/Non-Compliant Reports (2)");
    }

    private static Node CreateNode(string fqdn) => new()
    {
        Id = Guid.NewGuid(),
        Fqdn = fqdn,
        Status = NodeStatus.Compliant,
        ConfigurationSource = ConfigurationSource.Pull,
        CertificateThumbprint = "test-thumbprint",
        CertificateSubject = "CN=test",
        CertificateNotAfter = DateTimeOffset.UtcNow.AddYears(1),
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static Report CreateReport(Node node, bool inDesiredState, bool hadErrors) => new()
    {
        Id = Guid.NewGuid(),
        NodeId = node.Id,
        Node = node,
        Timestamp = DateTimeOffset.UtcNow,
        Operation = DscOperation.Test,
        InDesiredState = inDesiredState,
        HadErrors = hadErrors,
        ResultJson = "{}"
    };
}
