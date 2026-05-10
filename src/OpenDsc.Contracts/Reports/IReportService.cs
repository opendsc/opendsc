// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Reports;

/// <summary>
/// Read and submission operations for compliance reports.
/// </summary>
public interface IReportService
{
    Task<IReadOnlyList<ReportSummary>> GetReportsAsync(
        Guid? nodeId = null,
        int? skip = null,
        int? take = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default);

    Task<ReportDetails?> GetReportAsync(
        Guid reportId,
        CancellationToken cancellationToken = default);

    Task<Nodes.NodeSummary?> GetReportNodeAsync(
        Guid reportId,
        CancellationToken cancellationToken = default);

    Task<ReportSummary> SubmitReportAsync(
        Guid nodeId,
        SubmitReportRequest request,
        CancellationToken cancellationToken = default);
}
