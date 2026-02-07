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

public static class RegistrationKeyEndpoints
{
    public static void MapRegistrationKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/registration-keys")
            .RequireAuthorization(Permissions.RegistrationKeys_Manage)
            .WithTags("Registration Keys");

        group.MapPost("/", CreateRegistrationKey)
            .WithSummary("Create registration key")
            .WithDescription("Creates a new registration key with optional expiration and usage limits.");

        group.MapGet("/", GetRegistrationKeys)
            .WithSummary("List registration keys")
            .WithDescription("Returns all registration keys with their usage statistics.");

        group.MapDelete("/{keyId:guid}", RevokeRegistrationKey)
            .WithSummary("Revoke registration key")
            .WithDescription("Revokes a registration key, preventing further use.");
    }

    private static async Task<Ok<RegistrationKeyResponse>> CreateRegistrationKey(
        CreateRegistrationKeyRequest request,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var expiresAt = request.ExpiresAt ?? DateTimeOffset.UtcNow.AddDays(7);

        var key = KeyGenerator.GenerateRegistrationKey();
        var registrationKey = new RegistrationKey
        {
            Id = Guid.NewGuid(),
            Key = key,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            MaxUses = request.MaxUses,
            CurrentUses = 0,
            IsRevoked = false
        };

        db.RegistrationKeys.Add(registrationKey);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new RegistrationKeyResponse
        {
            Id = registrationKey.Id,
            Key = key,
            ExpiresAt = registrationKey.ExpiresAt,
            CreatedAt = registrationKey.CreatedAt,
            MaxUses = registrationKey.MaxUses,
            CurrentUses = registrationKey.CurrentUses,
            IsRevoked = registrationKey.IsRevoked
        });
    }

    private static async Task<Ok<List<RegistrationKeyResponse>>> GetRegistrationKeys(
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var keys = await db.RegistrationKeys
            .AsNoTracking()
            .Select(k => new RegistrationKeyResponse
            {
                Id = k.Id,
                Key = null,
                ExpiresAt = k.ExpiresAt,
                CreatedAt = k.CreatedAt,
                MaxUses = k.MaxUses,
                CurrentUses = k.CurrentUses,
                IsRevoked = k.IsRevoked
            })
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(keys);
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>> RevokeRegistrationKey(
        Guid keyId,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var key = await db.RegistrationKeys.FindAsync([keyId], cancellationToken);
        if (key is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Registration key not found." });
        }

        key.IsRevoked = true;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
