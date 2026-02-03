// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Endpoints;

public static class ScopeEndpoints
{
    public static IEndpointRouteBuilder MapScopeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/scopes")
            .WithTags("Scopes")
            .RequireAuthorization("Admin");

        group.MapGet("/", GetAllScopes)
            .WithName("GetAllScopes")
            .WithDescription("Get all parameter scopes ordered by precedence");

        group.MapGet("/{name}", GetScope)
            .WithName("GetScope")
            .WithDescription("Get a specific scope by name");

        group.MapPost("/", CreateScope)
            .WithName("CreateScope")
            .WithDescription("Create a new parameter scope");

        group.MapPut("/{name}", UpdateScope)
            .WithName("UpdateScope")
            .WithDescription("Update a scope's properties");

        group.MapPut("/reorder", ReorderScopes)
            .WithName("ReorderScopes")
            .WithDescription("Atomically reorder all scopes with new precedence values");

        group.MapDelete("/{name}", DeleteScope)
            .WithName("DeleteScope")
            .WithDescription("Delete a scope (only if no parameters exist for it)");

        return app;
    }

    private static async Task<Ok<List<ScopeDto>>> GetAllScopes(ServerDbContext db)
    {
        var scopes = await db.Scopes
            .OrderBy(s => s.Precedence)
            .Select(s => new ScopeDto
            {
                Name = s.Name,
                Description = s.Description,
                Precedence = s.Precedence,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync();

        return TypedResults.Ok(scopes);
    }

    private static async Task<Results<Ok<ScopeDto>, NotFound>> GetScope(
        string name,
        ServerDbContext db)
    {
        var scope = await db.Scopes
            .Where(s => s.Name == name)
            .Select(s => new ScopeDto
            {
                Name = s.Name,
                Description = s.Description,
                Precedence = s.Precedence,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return scope is not null
            ? TypedResults.Ok(scope)
            : TypedResults.NotFound();
    }

    private static async Task<Results<Created<ScopeDto>, BadRequest<string>, Conflict<string>>> CreateScope(
        [FromBody] CreateScopeRequest request,
        ServerDbContext db)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest("Scope name is required");
        }

        if (await db.Scopes.AnyAsync(s => s.Name == request.Name))
        {
            return TypedResults.Conflict($"Scope '{request.Name}' already exists");
        }

        if (await db.Scopes.AnyAsync(s => s.Precedence == request.Precedence))
        {
            return TypedResults.Conflict($"Precedence {request.Precedence} is already in use");
        }

        var scope = new Scope
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Precedence = request.Precedence,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Scopes.Add(scope);
        await db.SaveChangesAsync();

        var dto = new ScopeDto
        {
            Name = scope.Name,
            Description = scope.Description,
            Precedence = scope.Precedence,
            CreatedAt = scope.CreatedAt,
            UpdatedAt = scope.UpdatedAt
        };

        return TypedResults.Created($"/api/v1/scopes/{scope.Name}", dto);
    }

    private static async Task<Results<Ok<ScopeDto>, NotFound, BadRequest<string>>> UpdateScope(
        string name,
        [FromBody] UpdateScopeRequest request,
        ServerDbContext db)
    {
        var scope = await db.Scopes.FirstOrDefaultAsync(s => s.Name == name);
        if (scope is null)
        {
            return TypedResults.NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            scope.Description = request.Description;
        }

        if (request.Precedence.HasValue)
        {
            if (await db.Scopes.AnyAsync(s => s.Id != scope.Id && s.Precedence == request.Precedence.Value))
            {
                return TypedResults.BadRequest($"Precedence {request.Precedence} is already in use by another scope");
            }

            scope.Precedence = request.Precedence.Value;
        }

        scope.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var dto = new ScopeDto
        {
            Name = scope.Name,
            Description = scope.Description,
            Precedence = scope.Precedence,
            CreatedAt = scope.CreatedAt,
            UpdatedAt = scope.UpdatedAt
        };

        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Ok<List<ScopeDto>>, BadRequest<string>>> ReorderScopes(
        [FromBody] ReorderScopesRequest request,
        ServerDbContext db)
    {
        if (request.Scopes is null || request.Scopes.Count == 0)
        {
            return TypedResults.BadRequest("Scopes array is required");
        }

        var precedences = request.Scopes.Select(s => s.Precedence).ToList();
        if (precedences.Distinct().Count() != precedences.Count)
        {
            return TypedResults.BadRequest("Duplicate precedence values are not allowed");
        }

        var scopeNames = request.Scopes.Select(s => s.Name).ToList();
        var scopes = await db.Scopes
            .Where(s => scopeNames.Contains(s.Name))
            .ToListAsync();

        if (scopes.Count != request.Scopes.Count)
        {
            var missing = scopeNames.Except(scopes.Select(s => s.Name)).ToList();
            return TypedResults.BadRequest($"Scopes not found: {string.Join(", ", missing)}");
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var scopeUpdate in request.Scopes)
        {
            var scope = scopes.First(s => s.Name == scopeUpdate.Name);
            scope.Precedence = scopeUpdate.Precedence;
            scope.UpdatedAt = now;
        }

        await db.SaveChangesAsync();

        var result = scopes
            .OrderBy(s => s.Precedence)
            .Select(s => new ScopeDto
            {
                Name = s.Name,
                Description = s.Description,
                Precedence = s.Precedence,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToList();

        return TypedResults.Ok(result);
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>>> DeleteScope(
        string name,
        ServerDbContext db)
    {
        var scope = await db.Scopes
            .Include(s => s.ParameterVersions)
            .Include(s => s.NodeAssignments)
            .FirstOrDefaultAsync(s => s.Name == name);

        if (scope is null)
        {
            return TypedResults.NotFound();
        }

        if (scope.ParameterVersions.Count > 0)
        {
            return TypedResults.Conflict($"Cannot delete scope '{name}' because it has {scope.ParameterVersions.Count} parameter versions");
        }

        if (scope.NodeAssignments.Count > 0)
        {
            return TypedResults.Conflict($"Cannot delete scope '{name}' because it is assigned to {scope.NodeAssignments.Count} nodes");
        }

        db.Scopes.Remove(scope);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}

public sealed class ScopeDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int Precedence { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateScopeRequest
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int Precedence { get; init; }
}

public sealed class UpdateScopeRequest
{
    public string? Description { get; init; }
    public int? Precedence { get; init; }
}

public sealed class ReorderScopesRequest
{
    public required List<ScopeReorderItem> Scopes { get; init; }
}

public sealed class ScopeReorderItem
{
    public required string Name { get; init; }
    public required int Precedence { get; init; }
}
