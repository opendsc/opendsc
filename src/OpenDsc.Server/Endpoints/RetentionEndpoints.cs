// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using OpenDsc.Contracts.Retention;
using OpenDsc.Server.Authorization;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Endpoints;

public static class RetentionEndpoints
{
    public static void MapRetentionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/retention")
            .RequireAuthorization(RetentionPermissions.Manage)
            .WithTags("Retention");

        group.MapPost("/configurations/cleanup", CleanupConfigurationVersions)
            .WithSummary("Cleanup old configuration versions")
            .WithDescription("Removes configuration versions outside the retention policy. Keeps versions in active use by nodes.");

        group.MapPost("/parameters/cleanup", CleanupParameterVersions)
            .WithSummary("Cleanup old parameter versions")
            .WithDescription("Removes parameter file versions outside the retention policy. Keeps active parameter versions.");

        group.MapPost("/composite-configurations/cleanup", CleanupCompositeConfigurationVersions)
            .WithSummary("Cleanup old composite configuration versions")
            .WithDescription("Removes composite configuration versions outside the retention policy. Keeps versions in active use by nodes.");

        group.MapPost("/reports/cleanup", CleanupReports)
            .WithSummary("Cleanup old compliance reports")
            .WithDescription("Removes compliance reports outside the retention policy. Keeps the most recent reports per node.");

        group.MapPost("/status-events/cleanup", CleanupNodeStatusEvents)
            .WithSummary("Cleanup old LCM status events")
            .WithDescription("Removes LCM status events outside the retention policy. Keeps the most recent events per node.");

        group.MapGet("/runs", GetRunHistory)
            .WithSummary("Get retention run history")
            .WithDescription("Returns the most recent retention cleanup run records.");
    }

    private static async Task<Ok<VersionRetentionResult>> CleanupConfigurationVersions(
        [FromBody] CleanupRequest request,
        IVersionRetentionService retentionService,
        CancellationToken cancellationToken)
    {
        var result = await retentionService.CleanupConfigurationVersionsAsync(
            request.ToPolicy(),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Ok<VersionRetentionResult>> CleanupParameterVersions(
        [FromBody] CleanupRequest request,
        IVersionRetentionService retentionService,
        CancellationToken cancellationToken)
    {
        var result = await retentionService.CleanupParameterVersionsAsync(
            request.ToPolicy(),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Ok<VersionRetentionResult>> CleanupCompositeConfigurationVersions(
        [FromBody] CleanupRequest request,
        IVersionRetentionService retentionService,
        CancellationToken cancellationToken)
    {
        var result = await retentionService.CleanupCompositeConfigurationVersionsAsync(
            request.ToPolicy(),
            cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Ok<VersionRetentionResult>> CleanupReports(
        [FromBody] RecordCleanupRequest request,
        IVersionRetentionService retentionService,
        CancellationToken cancellationToken)
    {
        var result = await retentionService.CleanupReportsAsync(request.ToPolicy(), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<VersionRetentionResult>> CleanupNodeStatusEvents(
        [FromBody] RecordCleanupRequest request,
        IVersionRetentionService retentionService,
        CancellationToken cancellationToken)
    {
        var result = await retentionService.CleanupNodeStatusEventsAsync(request.ToPolicy(), cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<List<RetentionRunSummary>>> GetRunHistory(
        IVersionRetentionService retentionService,
        [FromQuery] int limit = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var runs = await retentionService.GetRunHistoryAsync(limit, from, to, cancellationToken);

        var dtos = runs.Select(r => new RetentionRunSummary
        {
            Id = r.Id,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            VersionType = r.VersionType,
            IsScheduled = r.IsScheduled,
            IsDryRun = r.IsDryRun,
            DeletedCount = r.DeletedCount,
            KeptCount = r.KeptCount,
            Error = r.Error
        }).ToList();

        return TypedResults.Ok(dtos);
    }
}

internal static class RetentionRequestExtensions
{
    internal static RetentionPolicy ToPolicy(this CleanupRequest request) => new()
    {
        KeepVersions = request.KeepVersions,
        KeepDays = request.KeepDays,
        KeepReleaseVersions = request.KeepReleaseVersions,
        DryRun = request.DryRun,
        IsScheduled = false
    };

    internal static RecordRetentionPolicy ToPolicy(this RecordCleanupRequest request) => new()
    {
        KeepCount = request.KeepCount,
        KeepDays = request.KeepDays,
        DryRun = request.DryRun,
        IsScheduled = false
    };
}
