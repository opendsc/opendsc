// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Contracts.Dashboard;
using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Reports;
using OpenDsc.Server.Data;

namespace OpenDsc.Server.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly ServerDbContext _db;

    public DashboardService(ServerDbContext db)
    {
        _db = db;
    }

    public async Task<NodeDashboardSummary> GetNodeSummaryAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _db.ServerSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var staleness = settings?.StalenessMultiplier ?? 2.0;
        var now = DateTimeOffset.UtcNow;

        var nodes = await _db.Nodes.AsNoTracking().ToListAsync(cancellationToken);
        var recentNodes = nodes
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .Select(n => new NodeSummary
            {
                Id = n.Id,
                Fqdn = n.Fqdn,
                ConfigurationName = n.ConfigurationName,
                Status = n.Status.ToString(),
                LcmStatus = n.LcmStatus.ToString(),
                IsStale = n.LastCheckIn.HasValue
                    && n.ConfigurationModeInterval.HasValue
                    && (now - n.LastCheckIn.Value) > n.ConfigurationModeInterval.Value * staleness,
                LastCheckIn = n.LastCheckIn,
                CreatedAt = n.CreatedAt,
                ConfigurationSource = n.ConfigurationSource,
                ConfigurationMode = n.ConfigurationMode,
                ConfigurationModeInterval = n.ConfigurationModeInterval,
                ReportCompliance = n.ReportCompliance,
                DesiredConfigurationMode = n.DesiredConfigurationMode,
                DesiredConfigurationModeInterval = n.DesiredConfigurationModeInterval,
                DesiredReportCompliance = n.DesiredReportCompliance
            })
            .ToList();

        return new NodeDashboardSummary
        {
            TotalCount = nodes.Count,
            CompliantCount = nodes.Count(n => n.Status == NodeStatus.Compliant),
            NonCompliantCount = nodes.Count(n => n.Status == NodeStatus.NonCompliant),
            ErrorCount = nodes.Count(n => n.Status == NodeStatus.Error),
            UnknownCount = nodes.Count(n => n.Status == NodeStatus.Unknown),
            LcmIdleCount = nodes.Count(n => n.LcmStatus == LcmStatus.Idle),
            LcmActiveCount = nodes.Count(n => n.LcmStatus is LcmStatus.Testing or LcmStatus.Remediating or LcmStatus.Downloading),
            LcmUnknownCount = nodes.Count(n => n.LcmStatus == LcmStatus.Unknown),
            LcmErrorCount = nodes.Count(n => n.LcmStatus == LcmStatus.Error),
            RecentNodes = recentNodes
        };
    }

    public async Task<ReportDashboardSummary> GetReportSummaryAsync(CancellationToken cancellationToken = default)
    {
        var recentReports = await _db.Reports
            .AsNoTracking()
            .Include(r => r.Node)
            .Select(r => new ReportSummary
            {
                Id = r.Id,
                NodeId = r.NodeId,
                NodeFqdn = r.Node.Fqdn,
                Timestamp = r.Timestamp,
                Operation = r.Operation,
                InDesiredState = r.InDesiredState,
                HadErrors = r.HadErrors
            })
            .ToListAsync(cancellationToken);

        return new ReportDashboardSummary
        {
            RecentReports = recentReports.OrderByDescending(r => r.Timestamp).Take(10).ToList()
        };
    }

    public async Task<StatusEventDashboardSummary> GetStatusEventSummaryAsync(CancellationToken cancellationToken = default)
    {
        var recentStatusEvents = await _db.NodeStatusEvents
            .AsNoTracking()
            .Include(e => e.Node)
            .Where(e => e.LcmStatus != null)
            .Select(e => new NodeStatusEventSummary
            {
                Id = e.Id,
                NodeId = e.NodeId,
                NodeFqdn = e.Node.Fqdn,
                LcmStatus = e.LcmStatus.HasValue ? e.LcmStatus.Value.ToString() : null,
                Timestamp = e.Timestamp
            })
            .ToListAsync(cancellationToken);

        return new StatusEventDashboardSummary
        {
            RecentStatusEvents = recentStatusEvents.OrderByDescending(e => e.Timestamp).Take(10).ToList()
        };
    }
}
