// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;

using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Authorization;

namespace OpenDsc.Server.Endpoints;

internal static class RetentionSettingsEndpoints
{
    public static RouteGroupBuilder MapRetentionSettingsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/settings/retention")
            .WithTags("Settings")
            .RequireAuthorization(ServerPermissions.SettingsWrite);

        group.MapGet("", GetRetentionSettings)
            .WithName("GetRetentionSettings")
            .WithSummary("Get global retention policy settings");

        group.MapPut("", UpdateRetentionSettings)
            .WithName("UpdateRetentionSettings")
            .WithSummary("Update global retention policy settings");

        return group;
    }

    private static async Task<Ok<RetentionSettingsResponse>> GetRetentionSettings(
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await settingsService.GetRetentionSettingsAsync(cancellationToken));
    }

    private static async Task<Results<Ok<RetentionSettingsResponse>, NotFound>> UpdateRetentionSettings(
        UpdateRetentionSettingsRequest request,
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(
                await settingsService.UpdateRetentionSettingsAsync(request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }
}
