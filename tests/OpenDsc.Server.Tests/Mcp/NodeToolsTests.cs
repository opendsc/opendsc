// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Lcm.Contracts;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Mcp;

using Xunit;

namespace OpenDsc.Server.Tests.Mcp;

[Trait("Category", "Unit")]
public class NodeToolsTests : IDisposable
{
    private readonly ServerDbContext _db;
    private readonly NodeTools _tools;

    public NodeToolsTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ServerDbContext(options);
        _tools = new NodeTools(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetNonCompliantNodes_ReturnsEmpty_WhenAllCompliant()
    {
        _db.Nodes.Add(CreateNode("node1.opendsc.dev", NodeStatus.Compliant));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetNonCompliantNodes(TestContext.Current.CancellationToken);

        result.Should().Contain("All nodes are compliant");
    }

    [Fact]
    public async Task GetNonCompliantNodes_ReturnsNonCompliantAndErrorNodes()
    {
        _db.Nodes.Add(CreateNode("good.opendsc.dev", NodeStatus.Compliant));
        _db.Nodes.Add(CreateNode("bad.opendsc.dev", NodeStatus.NonCompliant));
        _db.Nodes.Add(CreateNode("err.opendsc.dev", NodeStatus.Error));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetNonCompliantNodes(TestContext.Current.CancellationToken);

        result.Should().Contain("Non-Compliant Nodes (2)");
        result.Should().Contain("bad.opendsc.dev");
        result.Should().Contain("err.opendsc.dev");
        result.Should().NotContain("good.opendsc.dev");
    }

    [Fact]
    public async Task GetNodesByStatus_ReturnsFilteredNodes()
    {
        _db.Nodes.Add(CreateNode("node1.opendsc.dev", NodeStatus.Compliant));
        _db.Nodes.Add(CreateNode("node2.opendsc.dev", NodeStatus.NonCompliant));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetNodesByStatus("Compliant", TestContext.Current.CancellationToken);

        result.Should().Contain("Nodes with Status: Compliant (1)");
        result.Should().Contain("node1.opendsc.dev");
    }

    [Fact]
    public async Task GetNodesByStatus_ReturnsError_ForInvalidStatus()
    {
        var result = await _tools.GetNodesByStatus("Invalid", TestContext.Current.CancellationToken);

        result.Should().Contain("Invalid status");
    }

    [Fact]
    public async Task GetNodeDetails_ByFqdn_ReturnsDetails()
    {
        _db.Nodes.Add(CreateNode("web01.opendsc.dev", NodeStatus.Compliant, "WebServer"));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetNodeDetails("web01.opendsc.dev", TestContext.Current.CancellationToken);

        result.Should().Contain("web01.opendsc.dev");
        result.Should().Contain("WebServer");
        result.Should().Contain("Compliant");
    }

    [Fact]
    public async Task GetNodeDetails_ById_ReturnsDetails()
    {
        var node = CreateNode("web01.opendsc.dev", NodeStatus.Compliant);
        _db.Nodes.Add(node);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetNodeDetails(node.Id.ToString(), TestContext.Current.CancellationToken);

        result.Should().Contain("web01.opendsc.dev");
    }

    [Fact]
    public async Task GetNodeDetails_ReturnsNotFound_WhenNodeDoesNotExist()
    {
        var result = await _tools.GetNodeDetails("nonexistent.opendsc.dev", TestContext.Current.CancellationToken);

        result.Should().Contain("not found");
    }

    [Fact]
    public async Task GetStaleNodes_ReturnsEmpty_WhenNoneStale()
    {
        var node = CreateNode("node1.opendsc.dev", NodeStatus.Compliant);
        node.LastCheckIn = DateTimeOffset.UtcNow;
        node.ConfigurationModeInterval = TimeSpan.FromMinutes(30);
        _db.Nodes.Add(node);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetStaleNodes(TestContext.Current.CancellationToken);

        result.Should().Contain("No stale nodes");
    }

    [Fact]
    public async Task GetStaleNodes_ReturnsStaleNodes()
    {
        var node = CreateNode("stale.opendsc.dev", NodeStatus.Compliant);
        node.LastCheckIn = DateTimeOffset.UtcNow.AddHours(-3);
        node.ConfigurationModeInterval = TimeSpan.FromMinutes(30);
        _db.Nodes.Add(node);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetStaleNodes(TestContext.Current.CancellationToken);

        result.Should().Contain("Stale Nodes (1)");
        result.Should().Contain("stale.opendsc.dev");
    }

    [Fact]
    public async Task GetComplianceSummary_ReturnsCounts()
    {
        _db.Nodes.Add(CreateNode("c1.opendsc.dev", NodeStatus.Compliant));
        _db.Nodes.Add(CreateNode("c2.opendsc.dev", NodeStatus.Compliant));
        _db.Nodes.Add(CreateNode("nc.opendsc.dev", NodeStatus.NonCompliant));
        _db.Nodes.Add(CreateNode("err.opendsc.dev", NodeStatus.Error));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetComplianceSummary(TestContext.Current.CancellationToken);

        result.Should().Contain("4 total nodes");
        result.Should().Contain("| Compliant | 2 |");
        result.Should().Contain("| Non-Compliant | 1 |");
        result.Should().Contain("| Error | 1 |");
    }

    private static Node CreateNode(string fqdn, NodeStatus status, string? configName = null) => new()
    {
        Id = Guid.NewGuid(),
        Fqdn = fqdn,
        Status = status,
        ConfigurationName = configName,
        ConfigurationSource = ConfigurationSource.Pull,
        CertificateThumbprint = "test-thumbprint",
        CertificateSubject = "CN=test",
        CertificateNotAfter = DateTimeOffset.UtcNow.AddYears(1),
        CreatedAt = DateTimeOffset.UtcNow
    };
}
