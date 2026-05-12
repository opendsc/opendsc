// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Authorization;

namespace OpenDsc.Server.Endpoints;

public static class ScopeTypeEndpoints
{
    public static IEndpointRouteBuilder MapScopeTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/scope-types")
            .WithTags("Scope Types")
            .RequireAuthorization(ScopePermissions.AdminOverride);

        group.MapGet("/", GetAllScopeTypes)
            .WithName("GetAllScopeTypes")
            .WithDescription("Get all scope types ordered by precedence");

        group.MapGet("/{id:guid}", GetScopeType)
            .WithName("GetScopeType")
            .WithDescription("Get a specific scope type by ID");

        group.MapPost("/", CreateScopeType)
            .WithName("CreateScopeType")
            .WithDescription("Create a new scope type (auto-assigns precedence before Node scope)");

        group.MapPut("/{id:guid}", UpdateScopeType)
            .WithName("UpdateScopeType")
            .WithDescription("Update a scope type's properties");

        group.MapPut("/reorder", ReorderScopeTypes)
            .WithName("ReorderScopeTypes")
            .WithDescription("Atomically reorder all scope types (Default must be first, Node must be last if exists)");

        group.MapDelete("/{id:guid}", DeleteScopeType)
            .WithName("DeleteScopeType")
            .WithDescription("Delete a scope type (blocked if system scope, has scope values, or has parameters)");

        group.MapPatch("/{id:guid}/enable", EnableScopeType)
            .WithName("EnableScopeType")
            .WithDescription("Enable a system scope type so it participates in parameter merging");

        group.MapPatch("/{id:guid}/disable", DisableScopeType)
            .WithName("DisableScopeType")
            .WithDescription("Disable a system scope type (blocked if it has published parameter files)");

        return app;
    }

    private static async Task<Ok<List<ScopeTypeDetails>>> GetAllScopeTypes(
        IScopeService scopeService,
        CancellationToken cancellationToken)
    {
        var scopeTypes = await scopeService.GetScopeTypesAsync(cancellationToken);
        return TypedResults.Ok(scopeTypes.ToList());
    }

    private static async Task<Results<Ok<ScopeTypeDetails>, NotFound>> GetScopeType(
        Guid id,
        IScopeService scopeService,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await scopeService.GetScopeTypeAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Created<ScopeTypeDetails>, BadRequest<string>, Conflict<string>>> CreateScopeType(
        [FromBody] CreateScopeTypeRequest request,
        IScopeService scopeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var scopeType = await scopeService.CreateScopeTypeAsync(request, cancellationToken);
            return TypedResults.Created($"/api/v1/scope-types/{scopeType.Id}", scopeType);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(ex.Message);
        }
    }

    private static async Task<Results<Ok<ScopeTypeDetails>, NotFound, BadRequest<string>>> UpdateScopeType(
        Guid id,
        [FromBody] UpdateScopeTypeRequest request,
        IScopeService scopeService,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await scopeService.UpdateScopeTypeAsync(id, request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok<List<ScopeTypeDetails>>, BadRequest<string>>> ReorderScopeTypes(
        [FromBody] ReorderScopeTypesRequest request,
        IScopeService scopeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var scopeTypes = await scopeService.ReorderScopeTypesAsync(request, cancellationToken);
            return TypedResults.Ok(scopeTypes.ToList());
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>>> DeleteScopeType(
        Guid id,
        IScopeService scopeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await scopeService.DeleteScopeTypeAsync(id, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(ex.Message);
        }
    }

    private static async Task<Results<Ok<ScopeTypeDetails>, NotFound, BadRequest<string>>> EnableScopeType(
        Guid id,
        IScopeService scopeService,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await scopeService.EnableScopeTypeAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok<ScopeTypeDetails>, NotFound, BadRequest<string>>> DisableScopeType(
        Guid id,
        IScopeService scopeService,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await scopeService.DisableScopeTypeAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }
}
