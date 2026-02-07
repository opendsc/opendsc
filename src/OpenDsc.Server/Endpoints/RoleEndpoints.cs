// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Endpoints;

/// <summary>
/// Endpoints for role management.
/// </summary>
public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/roles")
            .WithTags("Roles")
            .RequireAuthorization(Permissions.Roles_Manage);

        group.MapGet("/", GetRoles)
            .WithSummary("List all roles")
            .WithDescription("Returns a list of all roles.");

        group.MapGet("/{id:guid}", GetRole)
            .WithSummary("Get role details")
            .WithDescription("Returns details for a specific role.");

        group.MapPost("/", CreateRole)
            .WithSummary("Create role")
            .WithDescription("Creates a new custom role.");

        group.MapPut("/{id:guid}", UpdateRole)
            .WithSummary("Update role")
            .WithDescription("Updates a custom role's details and permissions.");

        group.MapDelete("/{id:guid}", DeleteRole)
            .WithSummary("Delete role")
            .WithDescription("Deletes a custom role (system roles cannot be deleted).");
    }

    private static async Task<Ok<List<RoleSummaryDto>>> GetRoles(ServerDbContext db)
    {
        var roles = await db.Roles
            .OrderBy(r => r.Name)
            .Select(r => new RoleSummaryDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsSystemRole = r.IsSystemRole,
                CreatedAt = r.CreatedAt,
                ModifiedAt = r.ModifiedAt
            })
            .ToListAsync();

        return TypedResults.Ok(roles);
    }

    private static async Task<Results<Ok<RoleDetailDto>, NotFound>> GetRole(
        Guid id,
        ServerDbContext db)
    {
        var role = await db.Roles.FindAsync(id);
        if (role == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = JsonSerializer.Deserialize<string[]>(role.Permissions) ?? [],
            IsSystemRole = role.IsSystemRole,
            CreatedAt = role.CreatedAt,
            ModifiedAt = role.ModifiedAt
        });
    }

    private static async Task<Results<Created<RoleSummaryDto>, BadRequest<string>>> CreateRole(
        [FromBody] CreateRoleRequest request,
        ServerDbContext db)
    {
        if (await db.Roles.AnyAsync(r => r.Name == request.Name))
        {
            return TypedResults.BadRequest("Role name already exists");
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Permissions = JsonSerializer.Serialize(request.Permissions),
            IsSystemRole = false,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/v1/roles/{role.Id}", new RoleSummaryDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            CreatedAt = role.CreatedAt,
            ModifiedAt = role.ModifiedAt
        });
    }

    private static async Task<Results<Ok<RoleSummaryDto>, NotFound, BadRequest<string>>> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        ServerDbContext db)
    {
        var role = await db.Roles.FindAsync(id);
        if (role == null)
        {
            return TypedResults.NotFound();
        }

        if (role.IsSystemRole)
        {
            return TypedResults.BadRequest("Cannot modify system roles");
        }

        if (request.Name != role.Name &&
            await db.Roles.AnyAsync(r => r.Name == request.Name && r.Id != id))
        {
            return TypedResults.BadRequest("Role name already exists");
        }

        role.Name = request.Name;
        role.Description = request.Description;
        role.Permissions = JsonSerializer.Serialize(request.Permissions);
        role.ModifiedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return TypedResults.Ok(new RoleSummaryDto
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            CreatedAt = role.CreatedAt,
            ModifiedAt = role.ModifiedAt
        });
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> DeleteRole(
        Guid id,
        ServerDbContext db)
    {
        var role = await db.Roles.FindAsync(id);
        if (role == null)
        {
            return TypedResults.NotFound();
        }

        if (role.IsSystemRole)
        {
            return TypedResults.BadRequest("Cannot delete system roles");
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}

public sealed class RoleSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}

public sealed class RoleDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string[] Permissions { get; set; } = [];
    public bool IsSystemRole { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}

public sealed class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string[] Permissions { get; set; } = [];
}

public sealed class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string[] Permissions { get; set; } = [];
}
