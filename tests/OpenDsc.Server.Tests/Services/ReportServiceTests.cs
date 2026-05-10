// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

#pragma warning disable xUnit1051

using System.Text.Json;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Reports;
using OpenDsc.Schema;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class ReportServiceTests : IDisposable
{
    private readonly ServerDbContext _db;
    private readonly ReportService _service;

    public ReportServiceTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ServerDbContext(options);
        _service = new ReportService(_db, new JsonSerializerOptions());
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public async Task SubmitReportAsync_WithValidRequest_CreatesReportAndUpdatesNodeStatus()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        _db.Nodes.Add(new Node
        {
            Id = nodeId,
            Fqdn = "node1.contoso.test",
            CreatedAt = DateTimeOffset.UtcNow,
            Status = NodeStatus.Unknown,
            LcmStatus = LcmStatus.Idle
        });
        await _db.SaveChangesAsync();

        var resultElement = JsonDocument.Parse("{\"inDesiredState\":false}").RootElement;
        var request = new SubmitReportRequest
        {
            Operation = DscOperation.Test,
            Result = new DscResult
            {
                HadErrors = false,
                Results =
                [
                    new DscResourceResult
                    {
                        Type = "OpenDsc.FileSystem/File",
                        Name = "file1",
                        Result = resultElement
                    }
                ]
            }
        };

        // Act
        var summary = await _service.SubmitReportAsync(nodeId, request);

        // Assert
        summary.NodeId.Should().Be(nodeId);
        summary.InDesiredState.Should().BeFalse();
        summary.HadErrors.Should().BeFalse();

        var savedNode = await _db.Nodes.FindAsync(nodeId);
        savedNode.Should().NotBeNull();
        savedNode!.Status.Should().Be(NodeStatus.NonCompliant);
        savedNode.LastCheckIn.Should().NotBeNull();

        var savedReport = await _db.Reports.FirstOrDefaultAsync(r => r.Id == summary.Id);
        savedReport.Should().NotBeNull();
        savedReport!.Operation.Should().Be(DscOperation.Test);
    }

    [Fact]
    public async Task SubmitReportAsync_WithMissingNode_ThrowsKeyNotFound()
    {
        // Arrange
        var request = new SubmitReportRequest
        {
            Operation = DscOperation.Set,
            Result = new DscResult
            {
                HadErrors = false,
                Results = []
            }
        };

        // Act
        var action = async () => await _service.SubmitReportAsync(Guid.NewGuid(), request);

        // Assert
        await action.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Node not found*");
    }

    [Fact]
    public async Task GetReportsAsync_WithNodeFilter_ReturnsNewestFirst()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var otherNodeId = Guid.NewGuid();

        _db.Nodes.AddRange(
            new Node { Id = nodeId, Fqdn = "node1.contoso.test", CreatedAt = DateTimeOffset.UtcNow },
            new Node { Id = otherNodeId, Fqdn = "node2.contoso.test", CreatedAt = DateTimeOffset.UtcNow });

        _db.Reports.AddRange(
            new Report
            {
                Id = Guid.NewGuid(),
                NodeId = nodeId,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(-10),
                Operation = DscOperation.Test,
                InDesiredState = true,
                HadErrors = false,
                ResultJson = "{}"
            },
            new Report
            {
                Id = Guid.NewGuid(),
                NodeId = nodeId,
                Timestamp = DateTimeOffset.UtcNow,
                Operation = DscOperation.Set,
                InDesiredState = false,
                HadErrors = true,
                ResultJson = "{}"
            },
            new Report
            {
                Id = Guid.NewGuid(),
                NodeId = otherNodeId,
                Timestamp = DateTimeOffset.UtcNow,
                Operation = DscOperation.Get,
                InDesiredState = true,
                HadErrors = false,
                ResultJson = "{}"
            });

        await _db.SaveChangesAsync();

        // Act
        var reports = await _service.GetReportsAsync(nodeId: nodeId);

        // Assert
        reports.Should().HaveCount(2);
        reports[0].Timestamp.Should().BeAfter(reports[1].Timestamp);
        reports.Should().OnlyContain(r => r.NodeId == nodeId);
    }

    [Fact]
    public async Task GetReportNodeAsync_WithExistingReport_ReturnsNodeSummary()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var reportId = Guid.NewGuid();

        _db.ServerSettings.Add(new ServerSettings { StalenessMultiplier = 1.0 });
        _db.Nodes.Add(new Node
        {
            Id = nodeId,
            Fqdn = "node1.contoso.test",
            CreatedAt = DateTimeOffset.UtcNow,
            LastCheckIn = DateTimeOffset.UtcNow.AddHours(-2),
            ConfigurationModeInterval = TimeSpan.FromMinutes(30),
            Status = NodeStatus.Compliant,
            LcmStatus = LcmStatus.Idle,
            ConfigurationSource = ConfigurationSource.Pull
        });
        _db.Reports.Add(new Report
        {
            Id = reportId,
            NodeId = nodeId,
            Timestamp = DateTimeOffset.UtcNow,
            Operation = DscOperation.Test,
            InDesiredState = true,
            HadErrors = false,
            ResultJson = "{}"
        });

        await _db.SaveChangesAsync();

        // Act
        var summary = await _service.GetReportNodeAsync(reportId);

        // Assert
        summary.Should().NotBeNull();
        summary!.Id.Should().Be(nodeId);
        summary.Fqdn.Should().Be("node1.contoso.test");
        summary.IsStale.Should().BeTrue();
    }
}
