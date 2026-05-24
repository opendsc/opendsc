// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Globalization;

using OpenDsc.Client.Http;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Reports;

namespace OpenDsc.Client.Services;

/// <summary>
/// HTTP implementation of report operations.
/// </summary>
public sealed class ReportHttpService(HttpClient client)
    : HttpServiceBase(client), IReportService
{
    private static readonly ClientSerializerContext Ctx = ClientSerializerContext.Default;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReportSummary>> GetReportsAsync(
        Guid? nodeId = null,
        int? skip = null,
        int? take = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrlWithQuery(
            "api/v1/reports",
            ("nodeId", nodeId?.ToString()),
            ("skip", skip?.ToString()),
            ("take", take?.ToString()),
            ("from", from?.ToString("O", CultureInfo.InvariantCulture)),
            ("to", to?.ToString("O", CultureInfo.InvariantCulture)));

        return await GetAsync(url, Ctx.ReportSummaryList, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<ReportDetails?> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default)
        => GetOrNullAsync($"api/v1/reports/{reportId}", Ctx.ReportDetails, cancellationToken);

    /// <inheritdoc />
    public Task<NodeSummary?> GetReportNodeAsync(Guid reportId, CancellationToken cancellationToken = default)
        => GetOrNullAsync($"api/v1/reports/{reportId}/node", Ctx.NodeSummary, cancellationToken);

    /// <inheritdoc />
    public Task<ReportSummary> SubmitReportAsync(Guid nodeId, SubmitReportRequest request, CancellationToken cancellationToken = default)
        => PostAsync($"api/v1/nodes/{nodeId}/reports", request, Ctx.SubmitReportRequest, Ctx.ReportSummary, cancellationToken);
}
