// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authentication;
using OpenDsc.Server.Contracts;
using OpenDsc.Server.Data;

namespace OpenDsc.Server.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/settings")
            .RequireAuthorization("Admin")
            .WithTags("Settings");

        group.MapGet("/", GetSettings)
            .WithSummary("Get server settings")
            .WithDescription("Returns the current server settings.");

        group.MapPut("/", UpdateSettings)
            .WithSummary("Update server settings")
            .WithDescription("Updates server settings like key rotation interval.");

        group.MapPost("/registration-key/rotate", RotateRegistrationKey)
            .WithSummary("Rotate registration key")
            .WithDescription("Generates a new registration key, invalidating the old one.");
    }

    private static async Task<Results<Ok<ServerSettingsResponse>, NotFound<ErrorResponse>>> GetSettings(
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var settings = await db.ServerSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Server settings not found." });
        }

        return TypedResults.Ok(new ServerSettingsResponse
        {
            RegistrationKey = settings.RegistrationKey,
            KeyRotationInterval = settings.KeyRotationInterval
        });
    }

    private static async Task<Results<Ok<ServerSettingsResponse>, NotFound<ErrorResponse>>> UpdateSettings(
        UpdateServerSettingsRequest request,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var settings = await db.ServerSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Server settings not found." });
        }

        if (request.KeyRotationInterval.HasValue)
        {
            settings.KeyRotationInterval = request.KeyRotationInterval.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new ServerSettingsResponse
        {
            RegistrationKey = settings.RegistrationKey,
            KeyRotationInterval = settings.KeyRotationInterval
        });
    }

    private static async Task<Results<Ok<RotateRegistrationKeyResponse>, NotFound<ErrorResponse>>> RotateRegistrationKey(
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var settings = await db.ServerSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Server settings not found." });
        }

        settings.RegistrationKey = ApiKeyAuthHandler.GenerateApiKey();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new RotateRegistrationKeyResponse
        {
            RegistrationKey = settings.RegistrationKey
        });
    }
}
