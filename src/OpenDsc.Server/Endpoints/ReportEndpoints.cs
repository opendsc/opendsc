// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;

using Microsoft.AspNetCore.Http.HttpResults;

using OpenDsc.Contracts.Reports;
using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Authorization;

namespace OpenDsc.Server.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var nodeGroup = app.MapGroup("/api/v1/nodes/{nodeId:guid}/reports")
            .WithTags("Reports");

        nodeGroup.MapPost("/", SubmitReport)
            .RequireAuthorization("Node")
            .WithSummary("Submit compliance report")
            .WithDescription("Submits a compliance report from a node.");

        nodeGroup.MapGet("/", GetNodeReports)
            .RequireAuthorization(ReportPermissions.Read)
            .WithSummary("Get node reports")
            .WithDescription("Returns all compliance reports for a specific node.");

        var reportGroup = app.MapGroup("/api/v1/reports")
            .RequireAuthorization(ReportPermissions.ReadAll)
            .WithTags("Reports");

        reportGroup.MapGet("/", GetAllReports)
            .WithSummary("List all reports")
            .WithDescription("Returns a paginated list of all compliance reports.");

        reportGroup.MapGet("/{reportId:guid}", GetReport)
            .WithSummary("Get report details")
            .WithDescription("Returns the full details of a specific compliance report.");
    }

    private static async Task<Results<Created<ReportSummary>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>, ForbidHttpResult>> SubmitReport(
        Guid nodeId,
        SubmitReportRequest request,
        ClaimsPrincipal user,
        IReportService reportService,
        CancellationToken cancellationToken)
    {
        var authenticatedNodeId = user.FindFirst("node_id")?.Value;
        if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
        {
            return TypedResults.Forbid();
        }

        try
        {
            var summary = await reportService.SubmitReportAsync(nodeId, request, cancellationToken);
            return TypedResults.Created($"/api/v1/reports/{summary.Id}", summary);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<Ok<List<ReportSummary>>, NotFound<ErrorResponse>>> GetNodeReports(
        Guid nodeId,
        IReportService reportService,
        int? skip,
        int? take,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        try
        {
            var reports = await reportService.GetReportsAsync(nodeId, skip, take, from, to, cancellationToken);
            return TypedResults.Ok(reports.ToList());
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Ok<List<ReportSummary>>> GetAllReports(
        IReportService reportService,
        int? skip,
        int? take,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var reports = await reportService.GetReportsAsync(null, skip, take, from, to, cancellationToken);
        return TypedResults.Ok(reports.ToList());
    }

    private static async Task<Results<Ok<ReportDetails>, NotFound<ErrorResponse>>> GetReport(
        Guid reportId,
        IReportService reportService,
        CancellationToken cancellationToken)
    {
        var details = await reportService.GetReportAsync(reportId, cancellationToken);
        if (details is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Report not found." });
        }

        return TypedResults.Ok(details);
    }
}
