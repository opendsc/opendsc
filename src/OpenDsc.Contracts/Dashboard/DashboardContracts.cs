// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Reports;

namespace OpenDsc.Contracts.Dashboard;

/// <summary>
/// Aggregated node counts and recent node list for the dashboard.
/// </summary>
public sealed class NodeDashboardSummary
{
    /// <summary>
    /// Total registered nodes.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Count of compliant nodes.
    /// </summary>
    public int CompliantCount { get; set; }

    /// <summary>
    /// Count of non-compliant nodes.
    /// </summary>
    public int NonCompliantCount { get; set; }

    /// <summary>
    /// Count of nodes with error status.
    /// </summary>
    public int ErrorCount { get; set; }

    /// <summary>
    /// Count of nodes with unknown status.
    /// </summary>
    public int UnknownCount { get; set; }

    /// <summary>
    /// Count of nodes with idle LCM status.
    /// </summary>
    public int LcmIdleCount { get; set; }

    /// <summary>
    /// Count of nodes with active LCM status.
    /// </summary>
    public int LcmActiveCount { get; set; }

    /// <summary>
    /// Count of nodes with unknown LCM status.
    /// </summary>
    public int LcmUnknownCount { get; set; }

    /// <summary>
    /// Count of nodes with LCM error status.
    /// </summary>
    public int LcmErrorCount { get; set; }

    /// <summary>
    /// Most recent nodes.
    /// </summary>
    public IReadOnlyList<NodeSummary> RecentNodes { get; set; } = [];
}

/// <summary>
/// Aggregated report summary for dashboard visualization.
/// </summary>
public sealed class ReportDashboardSummary
{
    /// <summary>
    /// Recent report entries.
    /// </summary>
    public IReadOnlyList<ReportSummary> RecentReports { get; set; } = [];
}

/// <summary>
/// Aggregated node status event summary for dashboard visualization.
/// </summary>
public sealed class StatusEventDashboardSummary
{
    /// <summary>
    /// Recent node status events.
    /// </summary>
    public IReadOnlyList<NodeStatusEventSummary> RecentStatusEvents { get; set; } = [];
}
