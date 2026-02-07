// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Endpoints;

public static class ScopeValueEndpoints
{
    public static IEndpointRouteBuilder MapScopeValueEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/scope-types/{scopeTypeId:guid}/values")
            .WithTags("Scope Values")
            .RequireAuthorization(Permissions.Scopes_AdminOverride);

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

    private static async Task<Results<Ok<List<ScopeValueDto>>, NotFound>> GetScopeValues(
        Guid scopeTypeId,
        ServerDbContext db)
    {
        var scopeType = await db.ScopeTypes.FindAsync(scopeTypeId);
        if (scopeType is null)
        {
            return TypedResults.NotFound();
        }

        var values = await db.ScopeValues
            .Where(sv => sv.ScopeTypeId == scopeTypeId)
            .OrderBy(sv => sv.Value)
            .Select(sv => new ScopeValueDto
            {
                Id = sv.Id,
                ScopeTypeId = sv.ScopeTypeId,
                Value = sv.Value,
                Description = sv.Description,
                CreatedAt = sv.CreatedAt,
                UpdatedAt = sv.UpdatedAt
            })
            .ToListAsync();

        return TypedResults.Ok(values);
    }

    private static async Task<Results<Ok<ScopeValueDto>, NotFound>> GetScopeValue(
        Guid scopeTypeId,
        Guid id,
        ServerDbContext db)
    {
        var scopeValue = await db.ScopeValues
            .Where(sv => sv.Id == id && sv.ScopeTypeId == scopeTypeId)
            .Select(sv => new ScopeValueDto
            {
                Id = sv.Id,
                ScopeTypeId = sv.ScopeTypeId,
                Value = sv.Value,
                Description = sv.Description,
                CreatedAt = sv.CreatedAt,
                UpdatedAt = sv.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return scopeValue is not null
            ? TypedResults.Ok(scopeValue)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ScopeValueDto>, BadRequest<string>, NotFound, Conflict<string>>> CreateScopeValue(
        Guid scopeTypeId,
        [FromBody] CreateScopeValueRequest request,
        ServerDbContext db)
    {
        var scopeType = await db.ScopeTypes.FindAsync(scopeTypeId);
        if (scopeType is null)
        {
            return TypedResults.NotFound();
        }

        if (!scopeType.AllowsValues)
        {
            return TypedResults.BadRequest($"Scope type '{scopeType.Name}' does not allow values");
        }

        if (string.IsNullOrWhiteSpace(request.Value))
        {
            return TypedResults.BadRequest("Scope value is required");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Value, "^[a-zA-Z0-9_-]+$"))
        {
            return TypedResults.BadRequest("Scope value can only contain alphanumeric characters, hyphens, and underscores");
        }

        if (await db.ScopeValues.AnyAsync(sv => sv.ScopeTypeId == scopeTypeId && sv.Value == request.Value))
        {
            return TypedResults.Conflict($"Scope value '{request.Value}' already exists for this scope type");
        }

        var scopeValue = new ScopeValue
        {
            Id = Guid.NewGuid(),
            ScopeTypeId = scopeTypeId,
            Value = request.Value,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ScopeValues.Add(scopeValue);
        await db.SaveChangesAsync();

        var dto = new ScopeValueDto
        {
            Id = scopeValue.Id,
            ScopeTypeId = scopeValue.ScopeTypeId,
            Value = scopeValue.Value,
            Description = scopeValue.Description,
            CreatedAt = scopeValue.CreatedAt,
            UpdatedAt = scopeValue.UpdatedAt
        };

        return TypedResults.Created($"/api/v1/scope-types/{scopeTypeId}/values/{scopeValue.Id}", dto);
    }

    private static async Task<Results<Ok<ScopeValueDto>, NotFound, BadRequest<string>>> UpdateScopeValue(
        Guid scopeTypeId,
        Guid id,
        [FromBody] UpdateScopeValueRequest request,
        ServerDbContext db)
    {
        var scopeValue = await db.ScopeValues
            .FirstOrDefaultAsync(sv => sv.Id == id && sv.ScopeTypeId == scopeTypeId);

        if (scopeValue is null)
        {
            return TypedResults.NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            scopeValue.Description = request.Description;
        }

        scopeValue.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var dto = new ScopeValueDto
        {
            Id = scopeValue.Id,
            ScopeTypeId = scopeValue.ScopeTypeId,
            Value = scopeValue.Value,
            Description = scopeValue.Description,
            CreatedAt = scopeValue.CreatedAt,
            UpdatedAt = scopeValue.UpdatedAt
        };

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>>> DeleteScopeValue(
        Guid scopeTypeId,
        Guid id,
        ServerDbContext db)
    {
        var scopeValue = await db.ScopeValues
            .Include(sv => sv.NodeTags)
            .FirstOrDefaultAsync(sv => sv.Id == id && sv.ScopeTypeId == scopeTypeId);

        if (scopeValue is null)
        {
            return TypedResults.NotFound();
        }

        if (scopeValue.NodeTags.Count > 0)
        {
            return TypedResults.Conflict($"Cannot delete scope value '{scopeValue.Value}' because it is assigned to {scopeValue.NodeTags.Count} nodes");
        }

        var parameterCount = await db.ParameterFiles
            .CountAsync(pf => pf.ScopeTypeId == scopeTypeId && pf.ScopeValue == scopeValue.Value);

        if (parameterCount > 0)
        {
            return TypedResults.Conflict($"Cannot delete scope value '{scopeValue.Value}' because it has {parameterCount} parameter files");
        }

        db.ScopeValues.Remove(scopeValue);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}

public sealed class ScopeValueDto
{
    public required Guid Id { get; init; }
    public required Guid ScopeTypeId { get; init; }
    public required string Value { get; init; }
    public string? Description { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateScopeValueRequest
{
    public required string Value { get; init; }
    public string? Description { get; init; }
}

public sealed class UpdateScopeValueRequest
{
    public string? Description { get; init; }
}
