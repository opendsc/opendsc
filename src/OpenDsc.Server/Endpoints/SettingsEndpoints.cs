// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;

using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Authorization;

namespace OpenDsc.Server.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/settings/public", GetPublicSettings)
            .AllowAnonymous()
            .WithTags("Settings")
            .WithSummary("Get public server settings")
            .WithDescription("Returns settings that nodes need before authenticating, such as the certificate rotation interval.");

        var group = app.MapGroup("/api/v1/settings")
            .RequireAuthorization(ServerPermissions.SettingsWrite)
            .WithTags("Settings");

        group.MapGet("/", GetSettings)
            .WithSummary("Get server settings")
            .WithDescription("Returns the current server settings.");

        group.MapPut("/", UpdateSettings)
            .WithSummary("Update server settings")
            .WithDescription("Updates server settings like certificate rotation interval.");

        group.MapPost("/registration-keys", RotateRegistrationKey)
            .WithSummary("Rotate registration key")
            .WithDescription("Generates a new registration key for node registration.");

        group.MapGet("/lcm-defaults", GetLcmDefaults)
            .WithSummary("Get server LCM defaults")
            .WithDescription("Returns the server-wide default LCM settings applied to all nodes unless overridden at the node level.");

        group.MapPut("/lcm-defaults", UpdateLcmDefaults)
            .WithSummary("Update server LCM defaults")
            .WithDescription("Updates the server-wide default LCM settings. Null values clear the corresponding default.");
    }

    private static async Task<Results<Ok<PublicSettingsResponse>, NotFound<ErrorResponse>>> GetPublicSettings(
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await settingsService.GetPublicSettingsAsync(cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<Ok<ServerSettingsResponse>, NotFound<ErrorResponse>>> GetSettings(
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await settingsService.GetServerSettingsAsync(cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<Ok<ServerSettingsResponse>, NotFound<ErrorResponse>>> UpdateSettings(
        UpdateServerSettingsRequest request,
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(
                await settingsService.UpdateServerSettingsAsync(request, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Ok<RegistrationKeyResponse>> RotateRegistrationKey(
        IRegistrationKeyService registrationKeyService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await registrationKeyService.RotateKeyAsync(cancellationToken));
    }

    private static async Task<Results<Ok<ServerLcmDefaultsResponse>, NotFound<ErrorResponse>>> GetLcmDefaults(
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await settingsService.GetServerLcmDefaultsAsync(cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<Ok<ServerLcmDefaultsResponse>, NotFound<ErrorResponse>>> UpdateLcmDefaults(
        UpdateServerLcmDefaultsRequest request,
        ISettingsService settingsService,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(
                await settingsService.UpdateServerLcmDefaultsAsync(request, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = ex.Message });
        }
    }
}
