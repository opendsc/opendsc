// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Data;

namespace OpenDsc.Server.Services;

/// <summary>
/// Transforms claims to include permissions from user roles and group roles.
/// </summary>
public class GroupClaimsTransformation(ServerDbContext db) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return principal;
        }

        var permissions = await ResolveUserPermissionsAsync(userId, principal);

        var identity = new ClaimsIdentity();
        foreach (var permission in permissions)
        {
            identity.AddClaim(new Claim("permission", permission));
        }

        principal.AddIdentity(identity);
        return principal;
    }

    private async Task<HashSet<string>> ResolveUserPermissionsAsync(Guid userId, ClaimsPrincipal principal)
    {
        var userRolePermissions = await GetUserRolePermissionsAsync(userId);
        var internalGroupPermissions = await GetInternalGroupPermissionsAsync(userId);
        var externalGroupPermissions = await GetExternalGroupPermissionsAsync(principal);

        return userRolePermissions
            .Union(internalGroupPermissions)
            .Union(externalGroupPermissions)
            .ToHashSet();
    }

    private async Task<HashSet<string>> GetUserRolePermissionsAsync(Guid userId)
    {
        var permissions = new HashSet<string>();

        var roles = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r)
            .ToListAsync();

        foreach (var role in roles)
        {
            var rolePermissions = JsonSerializer.Deserialize<string[]>(role.Permissions) ?? [];
            foreach (var permission in rolePermissions)
            {
                permissions.Add(permission);
            }
        }

        return permissions;
    }

    private async Task<HashSet<string>> GetInternalGroupPermissionsAsync(Guid userId)
    {
        var permissions = new HashSet<string>();

        var groupIds = await db.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync();

        if (groupIds.Count == 0)
        {
            return permissions;
        }

        var roles = await db.GroupRoles
            .Where(gr => groupIds.Contains(gr.GroupId))
            .Join(db.Roles, gr => gr.RoleId, r => r.Id, (gr, r) => r)
            .ToListAsync();

        foreach (var role in roles)
        {
            var rolePermissions = JsonSerializer.Deserialize<string[]>(role.Permissions) ?? [];
            foreach (var permission in rolePermissions)
            {
                permissions.Add(permission);
            }
        }

        return permissions;
    }

    private async Task<HashSet<string>> GetExternalGroupPermissionsAsync(ClaimsPrincipal principal)
    {
        var permissions = new HashSet<string>();

        var externalGroupClaims = principal.FindAll("groups").Select(c => c.Value).ToList();
        if (externalGroupClaims.Count == 0)
        {
            return permissions;
        }

        var mappings = await db.ExternalGroupMappings
            .Where(egm => externalGroupClaims.Contains(egm.ExternalGroupId))
            .ToListAsync();

        if (mappings.Count == 0)
        {
            return permissions;
        }

        var groupIds = mappings.Select(m => m.GroupId).ToList();

        var roles = await db.GroupRoles
            .Where(gr => groupIds.Contains(gr.GroupId))
            .Join(db.Roles, gr => gr.RoleId, r => r.Id, (gr, r) => r)
            .ToListAsync();

        foreach (var role in roles)
        {
            var rolePermissions = JsonSerializer.Deserialize<string[]>(role.Permissions) ?? [];
            foreach (var permission in rolePermissions)
            {
                permissions.Add(permission);
            }
        }

        return permissions;
    }
}
