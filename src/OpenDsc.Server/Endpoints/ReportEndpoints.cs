// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Contracts;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

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
            .RequireAuthorization(Permissions.Reports_Read)
            .WithSummary("Get node reports")
            .WithDescription("Returns all compliance reports for a specific node.");

        var reportGroup = app.MapGroup("/api/v1/reports")
            .RequireAuthorization(Permissions.Reports_ReadAll)
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
        ServerDbContext db,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
    {
        var authenticatedNodeId = user.FindFirst("node_id")?.Value;
        if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
        {
            return TypedResults.Forbid();
        }

        if (request.Result is null)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Result is required." });
        }

        var node = await db.Nodes.FindAsync([nodeId], cancellationToken);
        if (node is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

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
            ResultJson = JsonSerializer.Serialize(request.Result, jsonOptions)
        };

        db.Reports.Add(report);

        node.LastCheckIn = DateTimeOffset.UtcNow;
        node.Status = report.HadErrors ? NodeStatus.Error
            : report.InDesiredState ? NodeStatus.Compliant
            : NodeStatus.NonCompliant;

        await db.SaveChangesAsync(cancellationToken);

        var summary = new ReportSummary
        {
            Id = report.Id,
            NodeId = report.NodeId,
            NodeFqdn = node.Fqdn,
            Timestamp = report.Timestamp,
            Operation = report.Operation,
            InDesiredState = report.InDesiredState,
            HadErrors = report.HadErrors
        };

        return TypedResults.Created($"/api/v1/reports/{report.Id}", summary);
    }

    private static async Task<Results<Ok<List<ReportSummary>>, NotFound<ErrorResponse>>> GetNodeReports(
        Guid nodeId,
        ServerDbContext db,
        int? skip,
        int? take,
        CancellationToken cancellationToken)
    {
        var nodeExists = await db.Nodes.AnyAsync(n => n.Id == nodeId, cancellationToken);
        if (!nodeExists)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        var reports = await db.Reports
            .AsNoTracking()
            .Include(r => r.Node)
            .Where(r => r.NodeId == nodeId)
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

        var orderedReports = reports
            .OrderByDescending(r => r.Timestamp)
            .Skip(skip ?? 0)
            .Take(take ?? 100)
            .ToList();

        return TypedResults.Ok(orderedReports);
    }

    private static async Task<Ok<List<ReportSummary>>> GetAllReports(
        ServerDbContext db,
        int? skip,
        int? take,
        CancellationToken cancellationToken)
    {
        var reports = await db.Reports
            .AsNoTracking()
            .Include(r => r.Node)
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

        var orderedReports = reports
            .OrderByDescending(r => r.Timestamp)
            .Skip(skip ?? 0)
            .Take(take ?? 100)
            .ToList();

        return TypedResults.Ok(orderedReports);
    }

    private static async Task<Results<Ok<ReportDetails>, NotFound<ErrorResponse>>> GetReport(
        Guid reportId,
        ServerDbContext db,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
    {
        var report = await db.Reports
            .AsNoTracking()
            .Include(r => r.Node)
            .FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);

        if (report is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Report not found." });
        }

        var details = new ReportDetails
        {
            Id = report.Id,
            NodeId = report.NodeId,
            NodeFqdn = report.Node?.Fqdn ?? string.Empty,
            Timestamp = report.Timestamp,
            Operation = report.Operation,
            InDesiredState = report.InDesiredState,
            HadErrors = report.HadErrors,
            Result = report.ResultJson is not null
                ? JsonSerializer.Deserialize<Schema.DscResult>(report.ResultJson, jsonOptions)
                : null
        };

        return TypedResults.Ok(details);
    }
}
