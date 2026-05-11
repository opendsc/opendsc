// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Authorization;

namespace OpenDsc.Server.Endpoints;

public static class ScopeValueEndpoints
{
    public static IEndpointRouteBuilder MapScopeValueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/scope-types/{scopeTypeId:guid}/values")
            .WithTags("Scope Values")
            .RequireAuthorization(ScopePermissions.AdminOverride);

        group.MapGet("/", GetScopeValues)
            .WithName("GetScopeValues")
            .WithDescription("Get all values for a scope type");

        group.MapGet("/{id:guid}", GetScopeValue)
            .WithName("GetScopeValue")
            .WithDescription("Get a specific scope value by ID");

        group.MapPost("/", CreateScopeValue)
            .WithName("CreateScopeValue")
            .WithDescription("Create a new scope value");

        group.MapPut("/{id:guid}", UpdateScopeValue)
            .WithName("UpdateScopeValue")
            .WithDescription("Update a scope value's description");

        group.MapDelete("/{id:guid}", DeleteScopeValue)
            .WithName("DeleteScopeValue")
            .WithDescription("Delete a scope value (blocked if nodes are tagged with it or parameters exist)");

        return app;
    }

    private static async Task<Results<Ok<List<ScopeValueDetails>>, NotFound>> GetScopeValues(
        Guid scopeTypeId,
        IScopeService scopeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var values = await scopeService.GetScopeValuesAsync(scopeTypeId, cancellationToken);
            return TypedResults.Ok(values.ToList());
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Ok<ScopeValueDetails>, NotFound>> GetScopeValue(
        Guid scopeTypeId,
        Guid id,
        IScopeService scopeService,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await scopeService.GetScopeValueAsync(scopeTypeId, id, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Created<ScopeValueDetails>, BadRequest<string>, NotFound, Conflict<string>>> CreateScopeValue(
        Guid scopeTypeId,
        [FromBody] CreateScopeValueRequest request,
        IScopeService scopeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var scopeValue = await scopeService.CreateScopeValueAsync(scopeTypeId, request, cancellationToken);
            return TypedResults.Created($"/api/v1/scope-types/{scopeTypeId}/values/{scopeValue.Id}", scopeValue);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
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

    private static async Task<Results<Ok<ScopeValueDetails>, NotFound, BadRequest<string>>> UpdateScopeValue(
        Guid scopeTypeId,
        Guid id,
        [FromBody] UpdateScopeValueRequest request,
        IScopeService scopeService,
        CancellationToken cancellationToken)
    {
        try
        {
            return TypedResults.Ok(await scopeService.UpdateScopeValueAsync(scopeTypeId, id, request, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>>> DeleteScopeValue(
        Guid scopeTypeId,
        Guid id,
        IScopeService scopeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await scopeService.DeleteScopeValueAsync(scopeTypeId, id, cancellationToken);
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
}
