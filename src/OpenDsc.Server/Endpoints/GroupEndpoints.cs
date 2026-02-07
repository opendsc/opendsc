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

/// <summary>
/// Endpoints for group management.
/// </summary>
public static class GroupEndpoints
{
    public static void MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/groups")
            .WithTags("Groups")
            .RequireAuthorization(Permissions.Groups_Manage);

        group.MapGet("/", GetGroups)
            .WithSummary("List all groups")
            .WithDescription("Returns a list of all groups.");

        group.MapGet("/{id:guid}", GetGroup)
            .WithSummary("Get group details")
            .WithDescription("Returns details for a specific group.");

        group.MapPost("/", CreateGroup)
            .WithSummary("Create group")
            .WithDescription("Creates a new group.");

        group.MapPut("/{id:guid}", UpdateGroup)
            .WithSummary("Update group")
            .WithDescription("Updates group details.");

        group.MapDelete("/{id:guid}", DeleteGroup)
            .WithSummary("Delete group")
            .WithDescription("Deletes a group.");

        group.MapGet("/{id:guid}/members", GetGroupMembers)
            .WithSummary("Get group members")
            .WithDescription("Returns the users who are members of this group.");

        group.MapPut("/{id:guid}/members", SetGroupMembers)
            .WithSummary("Set group members")
            .WithDescription("Sets the members of a group, replacing existing memberships.");

        group.MapGet("/{id:guid}/roles", GetGroupRoles)
            .WithSummary("Get group roles")
            .WithDescription("Returns the roles assigned to a group.");

        group.MapPut("/{id:guid}/roles", SetGroupRoles)
            .WithSummary("Set group roles")
            .WithDescription("Sets the roles for a group, replacing existing role assignments.");

        group.MapGet("/external-mappings", GetExternalGroupMappings)
            .WithSummary("List external group mappings")
            .WithDescription("Returns all external group mappings for SSO integration.");

        group.MapPost("/external-mappings", CreateExternalGroupMapping)
            .WithSummary("Create external group mapping")
            .WithDescription("Maps an external SSO group to an internal group.");

        group.MapDelete("/external-mappings/{id:guid}", DeleteExternalGroupMapping)
            .WithSummary("Delete external group mapping")
            .WithDescription("Removes an external group mapping.");
    }

    private static async Task<Ok<List<GroupSummaryDto>>> GetGroups(ServerDbContext db)
    {
        var groups = await db.Groups
            .OrderBy(g => g.Name)
            .Select(g => new GroupSummaryDto
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                CreatedAt = g.CreatedAt,
                ModifiedAt = g.ModifiedAt
            })
            .ToListAsync();

        return TypedResults.Ok(groups);
    }

    private static async Task<Results<Ok<GroupDetailDto>, NotFound>> GetGroup(
        Guid id,
        ServerDbContext db)
    {
        var group = await db.Groups.FindAsync(id);
        if (group == null)
        {
            return TypedResults.NotFound();
        }

        var memberIds = await db.UserGroups
            .Where(ug => ug.GroupId == id)
            .Select(ug => ug.UserId)
            .ToListAsync();

        var members = await db.Users
            .Where(u => memberIds.Contains(u.Id))
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                AccountType = u.AccountType.ToString(),
                IsActive = u.IsActive,
                RequirePasswordChange = u.RequirePasswordChange,
                LockoutEnd = u.LockoutEnd,
                CreatedAt = u.CreatedAt,
                ModifiedAt = u.ModifiedAt
            })
            .ToListAsync();

        var roleIds = await db.GroupRoles
            .Where(gr => gr.GroupId == id)
            .Select(gr => gr.RoleId)
            .ToListAsync();

        var roles = await db.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                IsSystemRole = r.IsSystemRole
            })
            .ToListAsync();

        return TypedResults.Ok(new GroupDetailDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            CreatedAt = group.CreatedAt,
            ModifiedAt = group.ModifiedAt,
            Members = members,
            Roles = roles
        });
    }

    private static async Task<Results<Created<GroupSummaryDto>, BadRequest<string>>> CreateGroup(
        [FromBody] CreateGroupRequest request,
        ServerDbContext db)
    {
        if (await db.Groups.AnyAsync(g => g.Name == request.Name))
        {
            return TypedResults.BadRequest("Group name already exists");
        }

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        db.Groups.Add(group);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/v1/groups/{group.Id}", new GroupSummaryDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            CreatedAt = group.CreatedAt,
            ModifiedAt = group.ModifiedAt
        });
    }

    private static async Task<Results<Ok<GroupSummaryDto>, NotFound, BadRequest<string>>> UpdateGroup(
        Guid id,
        [FromBody] UpdateGroupRequest request,
        ServerDbContext db)
    {
        var group = await db.Groups.FindAsync(id);
        if (group == null)
        {
            return TypedResults.NotFound();
        }

        if (request.Name != group.Name &&
            await db.Groups.AnyAsync(g => g.Name == request.Name && g.Id != id))
        {
            return TypedResults.BadRequest("Group name already exists");
        }

        group.Name = request.Name;
        group.Description = request.Description;
        group.ModifiedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return TypedResults.Ok(new GroupSummaryDto
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            CreatedAt = group.CreatedAt,
            ModifiedAt = group.ModifiedAt
        });
    }

    private static async Task<Results<NoContent, NotFound>> DeleteGroup(
        Guid id,
        ServerDbContext db)
    {
        var group = await db.Groups.FindAsync(id);
        if (group == null)
        {
            return TypedResults.NotFound();
        }

        db.Groups.Remove(group);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<List<UserDto>>, NotFound>> GetGroupMembers(
        Guid id,
        ServerDbContext db)
    {
        if (!await db.Groups.AnyAsync(g => g.Id == id))
        {
            return TypedResults.NotFound();
        }

        var userIds = await db.UserGroups
            .Where(ug => ug.GroupId == id)
            .Select(ug => ug.UserId)
            .ToListAsync();

        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                AccountType = u.AccountType.ToString(),
                IsActive = u.IsActive,
                RequirePasswordChange = u.RequirePasswordChange,
                LockoutEnd = u.LockoutEnd,
                CreatedAt = u.CreatedAt,
                ModifiedAt = u.ModifiedAt
            })
            .ToListAsync();

        return TypedResults.Ok(users);
    }

    private static async Task<Results<Ok, NotFound>> SetGroupMembers(
        Guid id,
        [FromBody] SetMembersRequest request,
        ServerDbContext db)
    {
        if (!await db.Groups.AnyAsync(g => g.Id == id))
        {
            return TypedResults.NotFound();
        }

        var existingMembers = await db.UserGroups
            .Where(ug => ug.GroupId == id)
            .ToListAsync();

        db.UserGroups.RemoveRange(existingMembers);

        var newMembers = request.UserIds.Select(userId => new UserGroup
        {
            GroupId = id,
            UserId = userId
        });

        db.UserGroups.AddRange(newMembers);
        await db.SaveChangesAsync();

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<List<RoleDto>>, NotFound>> GetGroupRoles(
        Guid id,
        ServerDbContext db)
    {
        if (!await db.Groups.AnyAsync(g => g.Id == id))
        {
            return TypedResults.NotFound();
        }

        var roleIds = await db.GroupRoles
            .Where(gr => gr.GroupId == id)
            .Select(gr => gr.RoleId)
            .ToListAsync();

        var roles = await db.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                IsSystemRole = r.IsSystemRole
            })
            .ToListAsync();

        return TypedResults.Ok(roles);
    }

    private static async Task<Results<Ok, NotFound>> SetGroupRoles(
        Guid id,
        [FromBody] SetRolesRequest request,
        ServerDbContext db)
    {
        if (!await db.Groups.AnyAsync(g => g.Id == id))
        {
            return TypedResults.NotFound();
        }

        var existingRoles = await db.GroupRoles
            .Where(gr => gr.GroupId == id)
            .ToListAsync();

        db.GroupRoles.RemoveRange(existingRoles);

        var newRoles = request.RoleIds.Select(roleId => new GroupRole
        {
            GroupId = id,
            RoleId = roleId
        });

        db.GroupRoles.AddRange(newRoles);
        await db.SaveChangesAsync();

        return TypedResults.Ok();
    }

    private static async Task<Ok<List<ExternalGroupMappingDto>>> GetExternalGroupMappings(
        ServerDbContext db)
    {
        var mappings = await db.ExternalGroupMappings
            .Select(m => new ExternalGroupMappingDto
            {
                Id = m.Id,
                Provider = m.Provider,
                ExternalGroupId = m.ExternalGroupId,
                ExternalGroupName = m.ExternalGroupName,
                GroupId = m.GroupId,
                GroupName = string.Empty,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync();

        var groupIds = mappings.Select(m => m.GroupId).Distinct().ToList();
        var groups = await db.Groups.Where(g => groupIds.Contains(g.Id)).ToDictionaryAsync(g => g.Id, g => g.Name);

        foreach (var mapping in mappings.Where(m => groups.ContainsKey(m.GroupId)))
        {
            mapping.GroupName = groups[mapping.GroupId] ?? string.Empty;
        }

        return TypedResults.Ok(mappings);
    }

    private static async Task<Results<Created<ExternalGroupMappingDto>, NotFound, BadRequest<string>>> CreateExternalGroupMapping(
        [FromBody] CreateExternalGroupMappingRequest request,
        ServerDbContext db)
    {
        var group = await db.Groups.FindAsync(request.GroupId);
        if (group == null)
        {
            return TypedResults.NotFound();
        }

        if (await db.ExternalGroupMappings.AnyAsync(m =>
            m.Provider == request.Provider && m.ExternalGroupId == request.ExternalGroupId))
        {
            return TypedResults.BadRequest("External group mapping already exists");
        }

        var mapping = new ExternalGroupMapping
        {
            Id = Guid.NewGuid(),
            Provider = request.Provider,
            ExternalGroupId = request.ExternalGroupId,
            ExternalGroupName = request.ExternalGroupName,
            GroupId = request.GroupId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.ExternalGroupMappings.Add(mapping);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/v1/groups/external-mappings/{mapping.Id}",
            new ExternalGroupMappingDto
            {
                Id = mapping.Id,
                Provider = mapping.Provider,
                ExternalGroupId = mapping.ExternalGroupId,
                ExternalGroupName = mapping.ExternalGroupName,
                GroupId = mapping.GroupId,
                GroupName = group.Name,
                CreatedAt = mapping.CreatedAt
            });
    }

    private static async Task<Results<Ok, NotFound>> DeleteExternalGroupMapping(
        Guid id,
        ServerDbContext db)
    {
        var mapping = await db.ExternalGroupMappings.FindAsync(id);
        if (mapping == null)
        {
            return TypedResults.NotFound();
        }

        db.ExternalGroupMappings.Remove(mapping);
        await db.SaveChangesAsync();

        return TypedResults.Ok();
    }
}

public sealed class GroupSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}

public sealed class GroupDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public List<UserDto> Members { get; set; } = [];
    public List<RoleDto> Roles { get; set; } = [];
}

public sealed class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class UpdateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class SetMembersRequest
{
    public Guid[] UserIds { get; set; } = [];
}

public sealed class ExternalGroupMappingDto
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ExternalGroupId { get; set; } = string.Empty;
    public string? ExternalGroupName { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CreateExternalGroupMappingRequest
{
    public string Provider { get; set; } = string.Empty;
    public string ExternalGroupId { get; set; } = string.Empty;
    public string? ExternalGroupName { get; set; }
    public Guid GroupId { get; set; }
}
