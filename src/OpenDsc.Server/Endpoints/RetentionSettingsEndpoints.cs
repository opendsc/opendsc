// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;

namespace OpenDsc.Server.Endpoints;

internal static class RetentionSettingsEndpoints
{
    public static RouteGroupBuilder MapRetentionSettingsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/settings/retention")
            .WithTags("Settings")
            .RequireAuthorization(Permissions.ServerSettings_Write);

        group.MapGet("", GetRetentionSettings)
            .WithName("GetRetentionSettings")
            .WithSummary("Get global retention policy settings");

        group.MapPut("", UpdateRetentionSettings)
            .WithName("UpdateRetentionSettings")
            .WithSummary("Update global retention policy settings");

        return group;
    }

    private static async Task<Ok<RetentionSettingsDto>> GetRetentionSettings(ServerDbContext db)
    {
        var settings = await db.ServerSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            return TypedResults.Ok(new RetentionSettingsDto());
        }

        return TypedResults.Ok(new RetentionSettingsDto
        {
            Enabled = settings.RetentionEnabled,
            KeepVersions = settings.RetentionKeepVersions,
            KeepDays = settings.RetentionKeepDays,
            KeepReleaseVersions = settings.RetentionKeepReleaseVersions,
            ScheduleIntervalHours = (int)settings.RetentionScheduleInterval.TotalHours,
            ReportKeepCount = settings.RetentionReportKeepCount,
            ReportKeepDays = settings.RetentionReportKeepDays,
            StatusEventKeepCount = settings.RetentionStatusEventKeepCount,
            StatusEventKeepDays = settings.RetentionStatusEventKeepDays
        });
    }

    private static async Task<Results<Ok<RetentionSettingsDto>, NotFound>> UpdateRetentionSettings(
        UpdateRetentionSettingsRequest request,
        ServerDbContext db)
    {
        var settings = await db.ServerSettings.FirstOrDefaultAsync();
        if (settings is null)
        {
            return TypedResults.NotFound();
        }

        if (request.Enabled.HasValue)
        {
            settings.RetentionEnabled = request.Enabled.Value;
        }

        if (request.KeepVersions.HasValue)
        {
            settings.RetentionKeepVersions = request.KeepVersions.Value;
        }

        if (request.KeepDays.HasValue)
        {
            settings.RetentionKeepDays = request.KeepDays.Value;
        }

        if (request.KeepReleaseVersions.HasValue)
        {
            settings.RetentionKeepReleaseVersions = request.KeepReleaseVersions.Value;
        }

        if (request.ScheduleIntervalHours.HasValue)
        {
            settings.RetentionScheduleInterval = TimeSpan.FromHours(request.ScheduleIntervalHours.Value);
        }

        if (request.ReportKeepCount.HasValue)
        {
            settings.RetentionReportKeepCount = request.ReportKeepCount.Value;
        }

        if (request.ReportKeepDays.HasValue)
        {
            settings.RetentionReportKeepDays = request.ReportKeepDays.Value;
        }

        if (request.StatusEventKeepCount.HasValue)
        {
            settings.RetentionStatusEventKeepCount = request.StatusEventKeepCount.Value;
        }

        if (request.StatusEventKeepDays.HasValue)
        {
            settings.RetentionStatusEventKeepDays = request.StatusEventKeepDays.Value;
        }

        await db.SaveChangesAsync();

        return TypedResults.Ok(new RetentionSettingsDto
        {
            Enabled = settings.RetentionEnabled,
            KeepVersions = settings.RetentionKeepVersions,
            KeepDays = settings.RetentionKeepDays,
            KeepReleaseVersions = settings.RetentionKeepReleaseVersions,
            ScheduleIntervalHours = (int)settings.RetentionScheduleInterval.TotalHours,
            ReportKeepCount = settings.RetentionReportKeepCount,
            ReportKeepDays = settings.RetentionReportKeepDays,
            StatusEventKeepCount = settings.RetentionStatusEventKeepCount,
            StatusEventKeepDays = settings.RetentionStatusEventKeepDays
        });
    }
}

/// <summary>
/// Global retention policy settings returned by the API.
/// </summary>
public sealed class RetentionSettingsDto
{
    public bool Enabled { get; init; } = false;
    public int KeepVersions { get; init; } = 10;
    public int KeepDays { get; init; } = 90;
    public bool KeepReleaseVersions { get; init; } = true;
    public int ScheduleIntervalHours { get; init; } = 24;
    public int ReportKeepCount { get; init; } = 1000;
    public int ReportKeepDays { get; init; } = 30;
    public int StatusEventKeepCount { get; init; } = 200;
    public int StatusEventKeepDays { get; init; } = 7;
}

/// <summary>
/// Request to update global retention policy settings. Null fields leave the existing value unchanged.
/// </summary>
public sealed class UpdateRetentionSettingsRequest
{
    public bool? Enabled { get; init; }
    public int? KeepVersions { get; init; }
    public int? KeepDays { get; init; }
    public bool? KeepReleaseVersions { get; init; }
    public int? ScheduleIntervalHours { get; init; }
    public int? ReportKeepCount { get; init; }
    public int? ReportKeepDays { get; init; }
    public int? StatusEventKeepCount { get; init; }
    public int? StatusEventKeepDays { get; init; }
}
