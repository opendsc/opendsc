// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Dashboard;

/// <summary>
/// Read-only summary operations for the admin dashboard.
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets node compliance and staleness summary counts.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Node dashboard summary.</returns>
    Task<NodeDashboardSummary> GetNodeSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets compliance report summary counts.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Report dashboard summary.</returns>
    Task<ReportDashboardSummary> GetReportSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent LCM status event summary.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Status event dashboard summary.</returns>
    Task<StatusEventDashboardSummary> GetStatusEventSummaryAsync(CancellationToken cancellationToken = default);
}
