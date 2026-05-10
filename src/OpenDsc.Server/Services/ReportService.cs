// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Reports;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

public sealed class ReportService : IReportService
{
    private readonly ServerDbContext _db;
    private readonly JsonSerializerOptions _jsonOptions;

    public ReportService(
        ServerDbContext db,
        JsonSerializerOptions jsonOptions)
    {
        _db = db;
        _jsonOptions = jsonOptions;
    }

    public async Task<IReadOnlyList<ReportSummary>> GetReportsAsync(
        Guid? nodeId = null,
        int? skip = null,
        int? take = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        if (nodeId.HasValue)
        {
            var nodeExists = await _db.Nodes.AnyAsync(n => n.Id == nodeId.Value, cancellationToken);
            if (!nodeExists)
            {
                throw new KeyNotFoundException("Node not found.");
            }
        }

        var allReports = await _db.Reports
            .AsNoTracking()
            .Include(r => r.Node)
            .Where(r => !nodeId.HasValue || r.NodeId == nodeId.Value)
            .Where(r => from == null || r.Timestamp >= from)
            .Where(r => to == null || r.Timestamp <= to)
            .Select(r => new ReportSummary
            {
                Id = r.Id,
                NodeId = r.NodeId,
                NodeFqdn = r.Node!.Fqdn,
                Timestamp = r.Timestamp,
                Operation = r.Operation,
                InDesiredState = r.InDesiredState,
                HadErrors = r.HadErrors
            })
            .ToListAsync(cancellationToken);

        return allReports
            .OrderByDescending(r => r.Timestamp)
            .Skip(skip ?? 0)
            .Take(take ?? 100)
            .ToList();
    }

    public async Task<ReportDetails?> GetReportAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var report = await _db.Reports
            .AsNoTracking()
            .Include(r => r.Node)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            return null;
        }

        return new ReportDetails
        {
            Id = report.Id,
            NodeId = report.NodeId,
            NodeFqdn = report.Node?.Fqdn ?? string.Empty,
            Timestamp = report.Timestamp,
            Operation = report.Operation,
            InDesiredState = report.InDesiredState,
            HadErrors = report.HadErrors,
            Result = report.ResultJson is not null
                ? JsonSerializer.Deserialize<Schema.DscResult>(report.ResultJson, _jsonOptions)
                : null
        };
    }

    public async Task<NodeSummary?> GetReportNodeAsync(
        Guid reportId,
        CancellationToken cancellationToken = default)
    {
        var reportNode = await _db.Reports
            .AsNoTracking()
            .Where(r => r.Id == reportId)
            .Select(r => r.Node)
            .FirstOrDefaultAsync(cancellationToken);

        if (reportNode is null)
        {
            return null;
        }

        var settings = await _db.ServerSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var staleness = settings?.StalenessMultiplier ?? 2.0;
        var now = DateTimeOffset.UtcNow;

        return new NodeSummary
        {
            Id = reportNode.Id,
            Fqdn = reportNode.Fqdn,
            ConfigurationName = reportNode.ConfigurationName,
            Status = reportNode.Status.ToString(),
            LcmStatus = reportNode.LcmStatus.ToString(),
            IsStale = reportNode.LastCheckIn.HasValue
                && reportNode.ConfigurationModeInterval.HasValue
                && (now - reportNode.LastCheckIn.Value) > reportNode.ConfigurationModeInterval.Value * staleness,
            LastCheckIn = reportNode.LastCheckIn,
            CreatedAt = reportNode.CreatedAt,
            ConfigurationSource = reportNode.ConfigurationSource,
            ConfigurationMode = reportNode.ConfigurationMode,
            ConfigurationModeInterval = reportNode.ConfigurationModeInterval,
            ReportCompliance = reportNode.ReportCompliance,
            DesiredConfigurationMode = reportNode.DesiredConfigurationMode,
            DesiredConfigurationModeInterval = reportNode.DesiredConfigurationModeInterval,
            DesiredReportCompliance = reportNode.DesiredReportCompliance
        };
    }

    public async Task<ReportSummary> SubmitReportAsync(
        Guid nodeId,
        SubmitReportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Result is null)
        {
            throw new ArgumentException("Result is required.", nameof(request));
        }

        var node = await _db.Nodes.FindAsync([nodeId], cancellationToken)
            ?? throw new KeyNotFoundException("Node not found.");

        bool inDesiredState = request.Result.Results?.All(r =>
        {
            if (r.Result.TryGetProperty("inDesiredState", out var prop))
            {
                return prop.GetBoolean();
            }

            return true;
        }) ?? true;

        var report = new Report
        {
            Id = Guid.NewGuid(),
            NodeId = nodeId,
            Timestamp = DateTimeOffset.UtcNow,
            Operation = request.Operation,
            InDesiredState = inDesiredState,
            HadErrors = request.Result.HadErrors,
            ResultJson = JsonSerializer.Serialize(request.Result, _jsonOptions)
        };

        _db.Reports.Add(report);

        node.LastCheckIn = DateTimeOffset.UtcNow;
        node.Status = report.HadErrors ? NodeStatus.Error
            : report.InDesiredState ? NodeStatus.Compliant
            : NodeStatus.NonCompliant;

        await _db.SaveChangesAsync(cancellationToken);

        return new ReportSummary
        {
            Id = report.Id,
            NodeId = report.NodeId,
            NodeFqdn = node.Fqdn,
            Timestamp = report.Timestamp,
            Operation = report.Operation,
            InDesiredState = report.InDesiredState,
            HadErrors = report.HadErrors
        };
    }
}
