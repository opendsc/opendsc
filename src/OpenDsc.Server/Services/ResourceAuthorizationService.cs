// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

/// <summary>
/// Service for checking resource-level authorization.
/// </summary>
public interface IResourceAuthorizationService
{
    Task<bool> CanReadConfigurationAsync(Guid userId, Guid configurationId);
    Task<bool> CanModifyConfigurationAsync(Guid userId, Guid configurationId);
    Task<bool> CanManageConfigurationAsync(Guid userId, Guid configurationId);
    Task<bool> CanReadCompositeConfigurationAsync(Guid userId, Guid compositeConfigurationId);
    Task<bool> CanModifyCompositeConfigurationAsync(Guid userId, Guid compositeConfigurationId);
    Task<bool> CanManageCompositeConfigurationAsync(Guid userId, Guid compositeConfigurationId);
    Task<bool> CanReadParameterAsync(Guid userId, Guid parameterId);
    Task<bool> CanModifyParameterAsync(Guid userId, Guid parameterId);
    Task<bool> CanManageParameterAsync(Guid userId, Guid parameterId);
    Task<List<Guid>> GetReadableConfigurationIdsAsync(Guid userId);
    Task<List<Guid>> GetReadableCompositeConfigurationIdsAsync(Guid userId);
    Task<List<Guid>> GetReadableParameterIdsAsync(Guid userId);
    Task GrantConfigurationPermissionAsync(Guid configurationId, Guid principalId, PrincipalType principalType, ResourcePermission permission, Guid grantedByUserId);
    Task GrantCompositeConfigurationPermissionAsync(Guid compositeConfigurationId, Guid principalId, PrincipalType principalType, ResourcePermission permission, Guid grantedByUserId);
    Task GrantParameterPermissionAsync(Guid parameterId, Guid principalId, PrincipalType principalType, ResourcePermission permission, Guid grantedByUserId);
    Task RevokeConfigurationPermissionAsync(Guid configurationId, Guid principalId, PrincipalType principalType);
    Task RevokeCompositeConfigurationPermissionAsync(Guid compositeConfigurationId, Guid principalId, PrincipalType principalType);
    Task RevokeParameterPermissionAsync(Guid parameterId, Guid principalId, PrincipalType principalType);
    Task<List<ConfigurationPermission>> GetConfigurationAclAsync(Guid configurationId);
    Task<List<CompositeConfigurationPermission>> GetCompositeConfigurationAclAsync(Guid compositeConfigurationId);
    Task<List<ParameterPermission>> GetParameterAclAsync(Guid parameterId);
    Task<bool> HasGlobalPermissionAsync(Guid userId, string permission);
}

/// <summary>
/// Resource authorization service implementation.
/// </summary>
public class ResourceAuthorizationService(ServerDbContext db) : IResourceAuthorizationService
{
    public async Task<bool> CanReadConfigurationAsync(Guid userId, Guid configurationId)
    {
        if (await HasGlobalPermissionAsync(userId, Permissions.Configurations_AdminOverride))
        {
            return true;
        }

        return await HasResourcePermissionAsync(userId, configurationId, ResourcePermission.Read, GetConfigurationPermissions);
    }

    public async Task<bool> CanModifyConfigurationAsync(Guid userId, Guid configurationId)
    {
        if (await HasGlobalPermissionAsync(userId, Permissions.Configurations_AdminOverride))
        {
            return true;
        }

        return await HasResourcePermissionAsync(userId, configurationId, ResourcePermission.Modify, GetConfigurationPermissions);
    }

    public async Task<bool> CanManageConfigurationAsync(Guid userId, Guid configurationId)
    {
        if (await HasGlobalPermissionAsync(userId, Permissions.Configurations_AdminOverride))
        {
            return true;
        }

        return await HasResourcePermissionAsync(userId, configurationId, ResourcePermission.Manage, GetConfigurationPermissions);
    }

    public async Task<bool> CanReadCompositeConfigurationAsync(Guid userId, Guid compositeConfigurationId)
    {
        if (await HasGlobalPermissionAsync(userId, Permissions.CompositeConfigurations_AdminOverride))
        {
            return true;
        }

        return await HasResourcePermissionAsync(userId, compositeConfigurationId, ResourcePermission.Read, GetCompositeConfigurationPermissions);
    }

    public async Task<bool> CanModifyCompositeConfigurationAsync(Guid userId, Guid compositeConfigurationId)
    {
        if (await HasGlobalPermissionAsync(userId, Permissions.CompositeConfigurations_AdminOverride))
        {
            return true;
        }

        return await HasResourcePermissionAsync(userId, compositeConfigurationId, ResourcePermission.Modify, GetCompositeConfigurationPermissions);
    }

    public async Task<bool> CanManageCompositeConfigurationAsync(Guid userId, Guid compositeConfigurationId)
    {
        if (await HasGlobalPermissionAsync(userId, Permissions.CompositeConfigurations_AdminOverride))
        {
            return true;
        }

        return await HasResourcePermissionAsync(userId, compositeConfigurationId, ResourcePermission.Manage, GetCompositeConfigurationPermissions);
    }

    public async Task<bool> CanReadParameterAsync(Guid userId, Guid parameterId)
    {
        if (await HasGlobalPermissionAsync(userId, Permissions.Parameters_AdminOverride))
        {
            return true;
        }

        return await HasResourcePermissionAsync(userId, parameterId, ResourcePermission.Read, GetParameterPermissions);
    }

    public async Task<bool> CanModifyParameterAsync(Guid userId, Guid parameterId)
    {
        if (await HasGlobalPermissionAsync(userId, Permissions.Parameters_AdminOverride))
        {
            return true;
        }

        return await HasResourcePermissionAsync(userId, parameterId, ResourcePermission.Modify, GetParameterPermissions);
    }

    public async Task<bool> CanManageParameterAsync(Guid userId, Guid parameterId)
    {
        if (await HasGlobalPermissionAsync(userId, Permissions.Parameters_AdminOverride))
        {
            return true;
        }

        return await HasResourcePermissionAsync(userId, parameterId, ResourcePermission.Manage, GetParameterPermissions);
    }

    public async Task<List<Guid>> GetReadableConfigurationIdsAsync(Guid userId)
    {
        if (await HasGlobalPermissionAsync(userId, Permissions.Configurations_AdminOverride))
        {
            return await db.Configurations.Select(c => c.Id).ToListAsync();
        }

        var userGroupIds = await db.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync();

        return await db.ConfigurationPermissions
            .Where(p =>
                (p.PrincipalType == PrincipalType.User && p.PrincipalId == userId) ||
                (p.PrincipalType == PrincipalType.Group && userGroupIds.Contains(p.PrincipalId)))
            .Select(p => p.ConfigurationId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<Guid>> GetReadableCompositeConfigurationIdsAsync(Guid userId)
    {
        if (await HasGlobalPermissionAsync(userId, Permissions.CompositeConfigurations_AdminOverride))
        {
            return await db.CompositeConfigurations.Select(c => c.Id).ToListAsync();
        }

        var userGroupIds = await db.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync();

        return await db.CompositeConfigurationPermissions
            .Where(p =>
                (p.PrincipalType == PrincipalType.User && p.PrincipalId == userId) ||
                (p.PrincipalType == PrincipalType.Group && userGroupIds.Contains(p.PrincipalId)))
            .Select(p => p.CompositeConfigurationId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<Guid>> GetReadableParameterIdsAsync(Guid userId)
    {
        if (await HasGlobalPermissionAsync(userId, Permissions.Parameters_AdminOverride))
        {
            return await db.ParameterFiles.Select(p => p.Id).ToListAsync();
        }

        var userGroupIds = await db.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync();

        return await db.ParameterPermissions
            .Where(p =>
                (p.PrincipalType == PrincipalType.User && p.PrincipalId == userId) ||
                (p.PrincipalType == PrincipalType.Group && userGroupIds.Contains(p.PrincipalId)))
            .Select(p => p.ParameterId)
            .Distinct()
            .ToListAsync();
    }

    public async Task GrantConfigurationPermissionAsync(Guid configurationId, Guid principalId, PrincipalType principalType, ResourcePermission permission, Guid grantedByUserId)
    {
        var existing = await db.ConfigurationPermissions
            .FirstOrDefaultAsync(p => p.ConfigurationId == configurationId && p.PrincipalId == principalId && p.PrincipalType == principalType);

        if (existing != null)
        {
            existing.PermissionLevel = permission;
            existing.GrantedAt = DateTimeOffset.UtcNow;
            existing.GrantedByUserId = grantedByUserId;
        }
        else
        {
            db.ConfigurationPermissions.Add(new ConfigurationPermission
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configurationId,
                PrincipalId = principalId,
                PrincipalType = principalType,
                PermissionLevel = permission,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedByUserId = grantedByUserId
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task GrantCompositeConfigurationPermissionAsync(Guid compositeConfigurationId, Guid principalId, PrincipalType principalType, ResourcePermission permission, Guid grantedByUserId)
    {
        var existing = await db.CompositeConfigurationPermissions
            .FirstOrDefaultAsync(p => p.CompositeConfigurationId == compositeConfigurationId && p.PrincipalId == principalId && p.PrincipalType == principalType);

        if (existing != null)
        {
            existing.PermissionLevel = permission;
            existing.GrantedAt = DateTimeOffset.UtcNow;
            existing.GrantedByUserId = grantedByUserId;
        }
        else
        {
            db.CompositeConfigurationPermissions.Add(new CompositeConfigurationPermission
            {
                Id = Guid.NewGuid(),
                CompositeConfigurationId = compositeConfigurationId,
                PrincipalId = principalId,
                PrincipalType = principalType,
                PermissionLevel = permission,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedByUserId = grantedByUserId
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task GrantParameterPermissionAsync(Guid parameterId, Guid principalId, PrincipalType principalType, ResourcePermission permission, Guid grantedByUserId)
    {
        var existing = await db.ParameterPermissions
            .FirstOrDefaultAsync(p => p.ParameterId == parameterId && p.PrincipalId == principalId && p.PrincipalType == principalType);

        if (existing != null)
        {
            existing.PermissionLevel = permission;
            existing.GrantedAt = DateTimeOffset.UtcNow;
            existing.GrantedByUserId = grantedByUserId;
        }
        else
        {
            db.ParameterPermissions.Add(new ParameterPermission
            {
                Id = Guid.NewGuid(),
                ParameterId = parameterId,
                PrincipalId = principalId,
                PrincipalType = principalType,
                PermissionLevel = permission,
                GrantedAt = DateTimeOffset.UtcNow,
                GrantedByUserId = grantedByUserId
            });
        }

        await db.SaveChangesAsync();
    }

    public async Task RevokeConfigurationPermissionAsync(Guid configurationId, Guid principalId, PrincipalType principalType)
    {
        var permission = await db.ConfigurationPermissions
            .FirstOrDefaultAsync(p => p.ConfigurationId == configurationId && p.PrincipalId == principalId && p.PrincipalType == principalType);

        if (permission != null)
        {
            db.ConfigurationPermissions.Remove(permission);
            await db.SaveChangesAsync();
        }
    }

    public async Task RevokeCompositeConfigurationPermissionAsync(Guid compositeConfigurationId, Guid principalId, PrincipalType principalType)
    {
        var permission = await db.CompositeConfigurationPermissions
            .FirstOrDefaultAsync(p => p.CompositeConfigurationId == compositeConfigurationId && p.PrincipalId == principalId && p.PrincipalType == principalType);

        if (permission != null)
        {
            db.CompositeConfigurationPermissions.Remove(permission);
            await db.SaveChangesAsync();
        }
    }

    public async Task RevokeParameterPermissionAsync(Guid parameterId, Guid principalId, PrincipalType principalType)
    {
        var permission = await db.ParameterPermissions
            .FirstOrDefaultAsync(p => p.ParameterId == parameterId && p.PrincipalId == principalId && p.PrincipalType == principalType);

        if (permission != null)
        {
            db.ParameterPermissions.Remove(permission);
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<ConfigurationPermission>> GetConfigurationAclAsync(Guid configurationId)
    {
        return await db.ConfigurationPermissions
            .Where(p => p.ConfigurationId == configurationId)
            .ToListAsync();
    }

    public async Task<List<CompositeConfigurationPermission>> GetCompositeConfigurationAclAsync(Guid compositeConfigurationId)
    {
        return await db.CompositeConfigurationPermissions
            .Where(p => p.CompositeConfigurationId == compositeConfigurationId)
            .ToListAsync();
    }

    public async Task<List<ParameterPermission>> GetParameterAclAsync(Guid parameterId)
    {
        return await db.ParameterPermissions
            .Where(p => p.ParameterId == parameterId)
            .ToListAsync();
    }

    public async Task<bool> HasGlobalPermissionAsync(Guid userId, string permission)
    {
        var userRoles = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r)
            .ToListAsync();

        foreach (var role in userRoles)
        {
            var permissions = System.Text.Json.JsonSerializer.Deserialize<string[]>(role.Permissions) ?? [];
            if (permissions.Contains(permission) || permissions.Contains("*"))
            {
                return true;
            }
        }

        var userGroupIds = await db.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync();

        if (userGroupIds.Count == 0)
        {
            return false;
        }

        var groupRoles = await db.GroupRoles
            .Where(gr => userGroupIds.Contains(gr.GroupId))
            .Join(db.Roles, gr => gr.RoleId, r => r.Id, (gr, r) => r)
            .ToListAsync();

        foreach (var role in groupRoles)
        {
            var permissions = System.Text.Json.JsonSerializer.Deserialize<string[]>(role.Permissions) ?? [];
            if (permissions.Contains(permission) || permissions.Contains("*"))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> HasResourcePermissionAsync<TPermission>(
        Guid userId,
        Guid resourceId,
        ResourcePermission requiredLevel,
        Func<Guid, Task<List<TPermission>>> getPermissionsFunc)
        where TPermission : class
    {
        var userGroupIds = await db.UserGroups
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync();

        var permissions = await getPermissionsFunc(resourceId);

        foreach (var perm in permissions)
        {
            var principalType = (PrincipalType)(perm.GetType().GetProperty("PrincipalType")?.GetValue(perm) ?? PrincipalType.User);
            var principalId = (Guid)(perm.GetType().GetProperty("PrincipalId")?.GetValue(perm) ?? Guid.Empty);
            var permLevel = (ResourcePermission)(perm.GetType().GetProperty("PermissionLevel")?.GetValue(perm) ?? ResourcePermission.Read);

            if (principalType == PrincipalType.User && principalId == userId && permLevel >= requiredLevel)
            {
                return true;
            }

            if (principalType == PrincipalType.Group && userGroupIds.Contains(principalId) && permLevel >= requiredLevel)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<List<ConfigurationPermission>> GetConfigurationPermissions(Guid configurationId)
    {
        return await db.ConfigurationPermissions
            .Where(p => p.ConfigurationId == configurationId)
            .ToListAsync();
    }

    private async Task<List<CompositeConfigurationPermission>> GetCompositeConfigurationPermissions(Guid compositeConfigurationId)
    {
        return await db.CompositeConfigurationPermissions
            .Where(p => p.CompositeConfigurationId == compositeConfigurationId)
            .ToListAsync();
    }

    private async Task<List<ParameterPermission>> GetParameterPermissions(Guid parameterId)
    {
        return await db.ParameterPermissions
            .Where(p => p.ParameterId == parameterId)
            .ToListAsync();
    }
}
