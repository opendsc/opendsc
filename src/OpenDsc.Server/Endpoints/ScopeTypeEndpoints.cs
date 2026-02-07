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

public static class ScopeTypeEndpoints
{
    public static IEndpointRouteBuilder MapScopeTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/scope-types")
            .WithTags("Scope Types")
            .RequireAuthorization(Permissions.Scopes_AdminOverride);

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

        return app;
    }

    private static async Task<Ok<List<ScopeTypeDto>>> GetAllScopeTypes(ServerDbContext db)
    {
        var scopeTypes = await db.ScopeTypes
            .OrderBy(st => st.Precedence)
            .Select(st => new ScopeTypeDto
            {
                Id = st.Id,
                Name = st.Name,
                Description = st.Description,
                Precedence = st.Precedence,
                IsSystem = st.IsSystem,
                AllowsValues = st.AllowsValues,
                CreatedAt = st.CreatedAt,
                UpdatedAt = st.UpdatedAt
            })
            .ToListAsync();

        return TypedResults.Ok(scopeTypes);
    }

    private static async Task<Results<Ok<ScopeTypeDto>, NotFound>> GetScopeType(
        Guid id,
        ServerDbContext db)
    {
        var scopeType = await db.ScopeTypes
            .Where(st => st.Id == id)
            .Select(st => new ScopeTypeDto
            {
                Id = st.Id,
                Name = st.Name,
                Description = st.Description,
                Precedence = st.Precedence,
                IsSystem = st.IsSystem,
                AllowsValues = st.AllowsValues,
                CreatedAt = st.CreatedAt,
                UpdatedAt = st.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return scopeType is not null
            ? TypedResults.Ok(scopeType)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ScopeTypeDto>, BadRequest<string>, Conflict<string>>> CreateScopeType(
        [FromBody] CreateScopeTypeRequest request,
        ServerDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest("Scope type name is required");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(request.Name, "^[a-zA-Z0-9_-]+$"))
        {
            return TypedResults.BadRequest("Scope type name can only contain alphanumeric characters, hyphens, and underscores");
        }

        if (await db.ScopeTypes.AnyAsync(st => st.Name == request.Name))
        {
            return TypedResults.Conflict($"Scope type '{request.Name}' already exists");
        }

        var allScopeTypes = await db.ScopeTypes.OrderBy(st => st.Precedence).ToListAsync();
        var nodeScopeType = allScopeTypes.FirstOrDefault(st => st.Name == "Node");

        int newPrecedence;
        if (nodeScopeType != null)
        {
            newPrecedence = nodeScopeType.Precedence;
            foreach (var st in allScopeTypes.Where(st => st.Precedence >= newPrecedence))
            {
                st.Precedence++;
                st.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
        else
        {
            newPrecedence = allScopeTypes.Count > 0 ? allScopeTypes.Max(st => st.Precedence) + 1 : 0;
        }

        var scopeType = new ScopeType
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Precedence = newPrecedence,
            IsSystem = false,
            AllowsValues = request.AllowsValues ?? true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ScopeTypes.Add(scopeType);
        await db.SaveChangesAsync();

        var dto = new ScopeTypeDto
        {
            Id = scopeType.Id,
            Name = scopeType.Name,
            Description = scopeType.Description,
            Precedence = scopeType.Precedence,
            IsSystem = scopeType.IsSystem,
            AllowsValues = scopeType.AllowsValues,
            CreatedAt = scopeType.CreatedAt,
            UpdatedAt = scopeType.UpdatedAt
        };

        return TypedResults.Created($"/api/v1/scope-types/{scopeType.Id}", dto);
    }

    private static async Task<Results<Ok<ScopeTypeDto>, NotFound, BadRequest<string>>> UpdateScopeType(
        Guid id,
        [FromBody] UpdateScopeTypeRequest request,
        ServerDbContext db)
    {
        var scopeType = await db.ScopeTypes.FirstOrDefaultAsync(st => st.Id == id);
        if (scopeType is null)
        {
            return TypedResults.NotFound();
        }

        if (scopeType.IsSystem)
        {
            return TypedResults.BadRequest("Cannot update system scope types");
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            scopeType.Description = request.Description;
        }

        scopeType.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var dto = new ScopeTypeDto
        {
            Id = scopeType.Id,
            Name = scopeType.Name,
            Description = scopeType.Description,
            Precedence = scopeType.Precedence,
            IsSystem = scopeType.IsSystem,
            AllowsValues = scopeType.AllowsValues,
            CreatedAt = scopeType.CreatedAt,
            UpdatedAt = scopeType.UpdatedAt
        };

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<List<ScopeTypeDto>>, BadRequest<string>>> ReorderScopeTypes(
        [FromBody] ReorderScopeTypesRequest request,
        ServerDbContext db)
    {
        if (request.ScopeTypeIds is null || request.ScopeTypeIds.Count == 0)
        {
            return TypedResults.BadRequest("Scope type IDs array is required");
        }

        var scopeTypes = await db.ScopeTypes
            .Where(st => request.ScopeTypeIds.Contains(st.Id))
            .ToListAsync();

        if (scopeTypes.Count != request.ScopeTypeIds.Count)
        {
            return TypedResults.BadRequest("Some scope type IDs were not found");
        }

        var defaultScopeType = scopeTypes.FirstOrDefault(st => st.Name == "Default");
        if (defaultScopeType != null && request.ScopeTypeIds[0] != defaultScopeType.Id)
        {
            return TypedResults.BadRequest("Default scope type must be first (precedence 0)");
        }

        var nodeScopeType = scopeTypes.FirstOrDefault(st => st.Name == "Node");
        if (nodeScopeType != null && request.ScopeTypeIds[^1] != nodeScopeType.Id)
        {
            return TypedResults.BadRequest("Node scope type must be last (highest precedence)");
        }

        var now = DateTimeOffset.UtcNow;

        // Two-phase update to avoid unique constraint violations:
        // Phase 1: Set all to temporary negative values
        for (int i = 0; i < request.ScopeTypeIds.Count; i++)
        {
            var scopeType = scopeTypes.First(st => st.Id == request.ScopeTypeIds[i]);
            scopeType.Precedence = -(i + 1);
            scopeType.UpdatedAt = now;
        }
        await db.SaveChangesAsync();

        // Phase 2: Set to final values
        for (int i = 0; i < request.ScopeTypeIds.Count; i++)
        {
            var scopeType = scopeTypes.First(st => st.Id == request.ScopeTypeIds[i]);
            scopeType.Precedence = i;
        }
        await db.SaveChangesAsync();

        var result = scopeTypes
            .OrderBy(st => st.Precedence)
            .Select(st => new ScopeTypeDto
            {
                Id = st.Id,
                Name = st.Name,
                Description = st.Description,
                Precedence = st.Precedence,
                IsSystem = st.IsSystem,
                AllowsValues = st.AllowsValues,
                CreatedAt = st.CreatedAt,
                UpdatedAt = st.UpdatedAt
            })
            .ToList();

        return TypedResults.Ok(result);
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>>> DeleteScopeType(
        Guid id,
        ServerDbContext db)
    {
        var scopeType = await db.ScopeTypes
            .Include(st => st.ScopeValues)
            .Include(st => st.ParameterFiles)
            .FirstOrDefaultAsync(st => st.Id == id);

        if (scopeType is null)
        {
            return TypedResults.NotFound();
        }

        if (scopeType.IsSystem)
        {
            return TypedResults.Conflict($"Cannot delete system scope type '{scopeType.Name}'");
        }

        if (scopeType.ScopeValues.Count > 0)
        {
            return TypedResults.Conflict($"Cannot delete scope type '{scopeType.Name}' because it has {scopeType.ScopeValues.Count} scope values");
        }

        if (scopeType.ParameterFiles.Count > 0)
        {
            return TypedResults.Conflict($"Cannot delete scope type '{scopeType.Name}' because it has {scopeType.ParameterFiles.Count} parameter files");
        }

        var allScopeTypes = await db.ScopeTypes.OrderBy(st => st.Precedence).ToListAsync();
        var deletedPrecedence = scopeType.Precedence;

        db.ScopeTypes.Remove(scopeType);

        foreach (var st in allScopeTypes.Where(st => st.Precedence > deletedPrecedence))
        {
            st.Precedence--;
            st.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}

public sealed class ScopeTypeDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int Precedence { get; init; }
    public required bool IsSystem { get; init; }
    public required bool AllowsValues { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateScopeTypeRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public bool? AllowsValues { get; init; }
}

public sealed class UpdateScopeTypeRequest
{
    public string? Description { get; init; }
}

public sealed class ReorderScopeTypesRequest
{
    public required List<Guid> ScopeTypeIds { get; init; }
}
