// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Contracts.Users;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

public sealed class RoleService(ServerDbContext db) : IRoleService
{
    public async Task<IReadOnlyList<RoleSummary>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return roles.Select(ToRoleSummary).ToList();
    }

    public async Task<RoleDetails> GetRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        if (role is null)
        {
            throw new KeyNotFoundException("Role not found");
        }

        return new RoleDetails
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = ParsePermissions(role.Permissions),
            IsSystemRole = role.IsSystemRole,
            CreatedAt = role.CreatedAt,
            ModifiedAt = role.ModifiedAt
        };
    }

    public async Task<RoleSummary> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (await db.Roles.AnyAsync(r => r.Name == request.Name, cancellationToken))
        {
            throw new InvalidOperationException("Role name already exists");
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Permissions = JsonSerializer.Serialize(request.Permissions.OrderBy(p => p).ToArray()),
            IsSystemRole = false,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);

        return ToRoleSummary(role);
    }

    public async Task<RoleSummary> UpdateRoleAsync(
        Guid roleId,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role is null)
        {
            throw new KeyNotFoundException("Role not found");
        }

        if (role.IsSystemRole)
        {
            throw new InvalidOperationException("Cannot modify system roles");
        }

        if (request.Name != role.Name
            && await db.Roles.AnyAsync(r => r.Name == request.Name && r.Id != roleId, cancellationToken))
        {
            throw new InvalidOperationException("Role name already exists");
        }

        role.Name = request.Name;
        role.Description = request.Description;
        role.Permissions = JsonSerializer.Serialize(request.Permissions.OrderBy(p => p).ToArray());
        role.ModifiedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return ToRoleSummary(role);
    }

    public async Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role is null)
        {
            throw new KeyNotFoundException("Role not found");
        }

        if (role.IsSystemRole)
        {
            throw new InvalidOperationException("Cannot delete system roles");
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GroupSummary>?> GetGroupsForRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken))
        {
            return null;
        }

        var groupIds = await db.GroupRoles
            .AsNoTracking()
            .Where(gr => gr.RoleId == roleId)
            .Select(gr => gr.GroupId)
            .ToListAsync(cancellationToken);

        var groups = await db.Groups
            .AsNoTracking()
            .Where(g => groupIds.Contains(g.Id))
            .OrderBy(g => g.Name)
            .Select(g => GroupService.ToGroupSummary(g))
            .ToListAsync(cancellationToken);

        return groups;
    }

    public async Task SetGroupsForRoleAsync(
        Guid roleId,
        SetRoleGroupsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Roles.AnyAsync(r => r.Id == roleId, cancellationToken))
        {
            throw new KeyNotFoundException("Role not found");
        }

        var existing = await db.GroupRoles
            .Where(gr => gr.RoleId == roleId)
            .ToListAsync(cancellationToken);

        db.GroupRoles.RemoveRange(existing);

        var newRows = request.GroupIds
            .Distinct()
            .Select(groupId => new GroupRole
            {
                GroupId = groupId,
                RoleId = roleId
            });

        db.GroupRoles.AddRange(newRows);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetRoleUserCountsAsync(CancellationToken cancellationToken = default)
    {
        return await db.UserRoles
            .AsNoTracking()
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetRoleGroupCountsAsync(CancellationToken cancellationToken = default)
    {
        return await db.GroupRoles
            .AsNoTracking()
            .GroupBy(gr => gr.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);
    }

    internal static RoleSummary ToRoleSummary(Role role)
    {
        return new RoleSummary
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            Permissions = ParsePermissions(role.Permissions),
            IsSystemRole = role.IsSystemRole,
            CreatedAt = role.CreatedAt,
            ModifiedAt = role.ModifiedAt
        };
    }

    private static string[] ParsePermissions(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<string[]>(value) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
