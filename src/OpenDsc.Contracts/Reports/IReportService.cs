// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Reports;

/// <summary>
/// Read and submission operations for compliance reports.
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Gets compliance reports, optionally filtered by node and date range.
    /// </summary>
    /// <param name="nodeId">Optional node ID to filter reports from a specific node (or null for all nodes).</param>
    /// <param name="skip">Optional number of reports to skip (pagination).</param>
    /// <param name="take">Optional maximum number of reports to return (pagination).</param>
    /// <param name="from">Optional start date/time for filtering reports (inclusive).</param>
    /// <param name="to">Optional end date/time for filtering reports (inclusive).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of report summaries matching the criteria.</returns>
    Task<IReadOnlyList<ReportSummary>> GetReportsAsync(
        Guid? nodeId = null,
        int? skip = null,
        int? take = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information for a specific compliance report.
    /// </summary>
    /// <param name="reportId">The unique identifier of the report.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed report information, or null if the report is not found.</returns>
    Task<ReportDetails?> GetReportAsync(
        Guid reportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the node information for a specific report.
    /// </summary>
    /// <param name="reportId">The unique identifier of the report.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Summary information for the node that submitted the report, or null if not found.</returns>
    Task<Nodes.NodeSummary?> GetReportNodeAsync(
        Guid reportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits a compliance report from a node.
    /// </summary>
    /// <param name="nodeId">The unique identifier of the node submitting the report.</param>
    /// <param name="request">The report submission request containing compliance details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Summary information for the submitted report.</returns>
    Task<ReportSummary> SubmitReportAsync(
        Guid nodeId,
        SubmitReportRequest request,
        CancellationToken cancellationToken = default);
}
