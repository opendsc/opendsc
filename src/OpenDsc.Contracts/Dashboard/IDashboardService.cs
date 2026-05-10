// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Dashboard;

/// <summary>
/// Read-only summary operations for the admin dashboard.
/// </summary>
public interface IDashboardService
{
    Task<NodeDashboardSummary> GetNodeSummaryAsync(CancellationToken cancellationToken = default);

    Task<ReportDashboardSummary> GetReportSummaryAsync(CancellationToken cancellationToken = default);

    Task<StatusEventDashboardSummary> GetStatusEventSummaryAsync(CancellationToken cancellationToken = default);
}
