// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;

using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Authorization;

namespace OpenDsc.Server.Endpoints;

internal static class ValidationSettingsEndpoints
{
    public static RouteGroupBuilder MapValidationSettingsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/settings/validation")
            .WithTags("Settings")
            .RequireAuthorization(ServerPermissions.SettingsWrite);

        group.MapGet("", GetValidationSettings)
            .WithName("GetValidationSettings");

        group.MapPut("", UpdateValidationSettings)
            .WithName("UpdateValidationSettings");

        return group;
    }

    private static async Task<Ok<ValidationSettingsResponse>> GetValidationSettings(
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await settingsService.GetValidationSettingsAsync(cancellationToken));
    }

    private static async Task<Ok<ValidationSettingsResponse>> UpdateValidationSettings(
        UpdateValidationSettingsRequest request,
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(
            await settingsService.UpdateValidationSettingsAsync(request, cancellationToken));
    }
}
