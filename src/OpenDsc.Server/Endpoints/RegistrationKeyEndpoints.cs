// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;

using OpenDsc.Server.Authorization;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Server.Endpoints;

public static class RegistrationKeyEndpoints
{
    public static void MapRegistrationKeyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/registration-keys")
            .RequireAuthorization(ServerPermissions.RegistrationKeysManage)
            .WithTags("Registration Keys");

        group.MapPost("/", CreateRegistrationKey)
            .WithSummary("Create registration key")
            .WithDescription("Creates a new registration key with optional expiration and usage limits.");

        group.MapGet("/", GetRegistrationKeys)
            .WithSummary("List registration keys")
            .WithDescription("Returns all registration keys with their usage statistics.");

        group.MapPut("/{keyId:guid}", UpdateRegistrationKey)
            .WithSummary("Update registration key")
            .WithDescription("Updates the description of a registration key.");

        group.MapDelete("/{keyId:guid}", RevokeRegistrationKey)
            .WithSummary("Revoke registration key")
            .WithDescription("Revokes a registration key, preventing further use.");
    }

    private static async Task<Ok<RegistrationKeyResponse>> CreateRegistrationKey(
        CreateRegistrationKeyRequest request,
        IRegistrationKeyService keyService,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await keyService.CreateKeyAsync(request, cancellationToken));
    }

    private static async Task<Ok<List<RegistrationKeyResponse>>> GetRegistrationKeys(
        IRegistrationKeyService keyService,
        CancellationToken cancellationToken)
    {
        var keys = await keyService.GetKeysAsync(cancellationToken);
        return TypedResults.Ok(keys.ToList());
    }

    private static async Task<Results<Ok<RegistrationKeyResponse>, NotFound<ErrorResponse>>> UpdateRegistrationKey(
        Guid keyId,
        UpdateRegistrationKeyRequest request,
        IRegistrationKeyService keyService,
        CancellationToken cancellationToken)
    {
        try
        {
            var key = await keyService.UpdateKeyAsync(keyId, request, cancellationToken);
            return TypedResults.Ok(key);
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>> RevokeRegistrationKey(
        Guid keyId,
        IRegistrationKeyService keyService,
        CancellationToken cancellationToken)
    {
        try
        {
            await keyService.RevokeKeyAsync(keyId, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = ex.Message });
        }
    }
}
