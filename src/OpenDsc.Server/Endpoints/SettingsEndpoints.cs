// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Contracts;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Endpoints;

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/settings")
            .RequireAuthorization(Permissions.ServerSettings_Write)
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
            CertificateRotationInterval = settings.CertificateRotationInterval
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

        if (request.CertificateRotationInterval.HasValue)
        {
            settings.CertificateRotationInterval = request.CertificateRotationInterval.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new ServerSettingsResponse
        {
            CertificateRotationInterval = settings.CertificateRotationInterval
        });
    }

    private static async Task<Ok<RegistrationKeyResponse>> RotateRegistrationKey(
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var key = KeyGenerator.GenerateRegistrationKey();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);

        var registrationKey = new RegistrationKey
        {
            Id = Guid.NewGuid(),
            Key = key,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            MaxUses = null,
            CurrentUses = 0,
            IsRevoked = false
        };

        db.RegistrationKeys.Add(registrationKey);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new RegistrationKeyResponse
        {
            Id = registrationKey.Id,
            Key = key,
            ExpiresAt = expiresAt,
            MaxUses = null,
            CurrentUses = 0
        });
    }
}
