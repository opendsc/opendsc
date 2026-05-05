// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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

    private static async Task<Ok<List<RetentionRunDto>>> GetRunHistory(
        IVersionRetentionService retentionService,
        [FromQuery] int limit = 50,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var runs = await retentionService.GetRunHistoryAsync(limit, from, to, cancellationToken);

        var dtos = runs.Select(r => new RetentionRunDto
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

/// <summary>
/// Request to cleanup old versions.
/// </summary>
public sealed class CleanupRequest
{
    /// <summary>Number of recent versions to keep.</summary>
    public int KeepVersions { get; init; } = 10;

    /// <summary>Number of days to keep versions.</summary>
    public int KeepDays { get; init; } = 90;

    /// <summary>When true, release (non-prerelease) versions are never deleted.</summary>
    public bool KeepReleaseVersions { get; init; } = true;

    /// <summary>If true, returns what would be deleted without actually deleting.</summary>
    public bool DryRun { get; init; } = true;

    internal RetentionPolicy ToPolicy() => new()
    {
        KeepVersions = KeepVersions,
        KeepDays = KeepDays,
        KeepReleaseVersions = KeepReleaseVersions,
        DryRun = DryRun,
        IsScheduled = false
    };
}

/// <summary>
/// Request to cleanup old records (compliance reports or LCM status events).
/// </summary>
public sealed class RecordCleanupRequest
{
    /// <summary>Maximum number of records to keep per node.</summary>
    public int KeepCount { get; init; } = 1000;

    /// <summary>Number of days to keep records.</summary>
    public int KeepDays { get; init; } = 30;

    /// <summary>If true, returns what would be deleted without actually deleting.</summary>
    public bool DryRun { get; init; } = true;

    internal RecordRetentionPolicy ToPolicy() => new()
    {
        KeepCount = KeepCount,
        KeepDays = KeepDays,
        DryRun = DryRun,
        IsScheduled = false
    };
}

/// <summary>
/// Summary of a single retention cleanup run.
/// </summary>
public sealed class RetentionRunDto
{
    public required Guid Id { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public required string VersionType { get; init; }
    public required bool IsScheduled { get; init; }
    public required bool IsDryRun { get; init; }
    public required int DeletedCount { get; init; }
    public required int KeptCount { get; init; }
    public string? Error { get; init; }
}
