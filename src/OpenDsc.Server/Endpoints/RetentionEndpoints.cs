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
            .RequireAuthorization(Permissions.Retention_Manage)
            .WithTags("Retention");

        group.MapPost("/configurations/cleanup", CleanupConfigurationVersions)
            .WithSummary("Cleanup old configuration versions")
            .WithDescription("Removes configuration versions older than the retention policy. Keeps versions that are in active use by nodes.");

        group.MapPost("/parameters/cleanup", CleanupParameterVersions)
            .WithSummary("Cleanup old parameter versions")
            .WithDescription("Removes parameter versions older than the retention policy. Keeps active parameter versions.");
    }

    private static async Task<Ok<VersionRetentionResult>> CleanupConfigurationVersions(
        [FromBody] CleanupRequest request,
        IVersionRetentionService retentionService,
        CancellationToken cancellationToken)
    {
        var result = await retentionService.CleanupConfigurationVersionsAsync(
            request.KeepVersions,
            request.KeepDays,
            request.DryRun,
            cancellationToken);

        return TypedResults.Ok(result);
    }

    private static async Task<Ok<VersionRetentionResult>> CleanupParameterVersions(
        [FromBody] CleanupRequest request,
        IVersionRetentionService retentionService,
        CancellationToken cancellationToken)
    {
        var result = await retentionService.CleanupParameterVersionsAsync(
            request.KeepVersions,
            request.KeepDays,
            request.DryRun,
            cancellationToken);

        return TypedResults.Ok(result);
    }
}

/// <summary>
/// Request to cleanup old versions.
/// </summary>
public sealed class CleanupRequest
{
    /// <summary>
    /// Number of recent versions to keep.
    /// </summary>
    public int KeepVersions { get; init; } = 5;

    /// <summary>
    /// Number of days to keep versions.
    /// </summary>
    public int KeepDays { get; init; } = 30;

    /// <summary>
    /// If true, performs a dry-run and returns what would be deleted without actually deleting.
    /// </summary>
    public bool DryRun { get; init; } = true;
}
