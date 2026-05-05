// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Data;

namespace OpenDsc.Server.Services;

public static class RbacPermissionResolver
{
    public static async Task<HashSet<string>> ResolveUserAndInternalGroupPermissionsAsync(ServerDbContext db, Guid userId)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);

        var userRolePermissionJson = await db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Join(db.Roles.AsNoTracking(), ur => ur.RoleId, r => r.Id, (ur, r) => r.Permissions)
            .ToListAsync();

        AddPermissions(userRolePermissionJson, permissions);

        var internalGroupIds = await db.UserGroups
            .AsNoTracking()
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync();

        if (internalGroupIds.Count == 0)
        {
            return permissions;
        }

        var internalGroupRolePermissionJson = await db.GroupRoles
            .AsNoTracking()
            .Where(gr => internalGroupIds.Contains(gr.GroupId))
            .Join(db.Roles.AsNoTracking(), gr => gr.RoleId, r => r.Id, (gr, r) => r.Permissions)
            .ToListAsync();

        AddPermissions(internalGroupRolePermissionJson, permissions);

        return permissions;
    }

    private static void AddPermissions(IEnumerable<string> serializedPermissions, HashSet<string> permissions)
    {
        foreach (var serialized in serializedPermissions)
        {
            var rolePermissions = JsonSerializer.Deserialize<string[]>(serialized) ?? [];
            foreach (var permission in rolePermissions)
            {
                permissions.Add(permission);
            }
        }
    }
}
