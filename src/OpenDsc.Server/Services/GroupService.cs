// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Contracts.Users;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

public sealed class GroupService(ServerDbContext db) : IGroupService
{
    public async Task<IReadOnlyList<GroupSummary>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        var groups = await db.Groups
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);

        return groups.Select(ToGroupSummary).ToList();
    }

    public async Task<GroupDetails> GetGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await db.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);

        if (group is null)
        {
            throw new KeyNotFoundException("Group not found");
        }

        var memberIds = await db.UserGroups
            .AsNoTracking()
            .Where(ug => ug.GroupId == groupId)
            .Select(ug => ug.UserId)
            .ToListAsync(cancellationToken);

        var members = await db.Users
            .AsNoTracking()
            .Where(u => memberIds.Contains(u.Id))
            .OrderBy(u => u.Username)
            .Select(u => UserService.ToUserSummary(u))
            .ToListAsync(cancellationToken);

        var roleIds = await db.GroupRoles
            .AsNoTracking()
            .Where(gr => gr.GroupId == groupId)
            .Select(gr => gr.RoleId)
            .ToListAsync(cancellationToken);

        var roles = await db.Roles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .Select(r => RoleService.ToRoleSummary(r))
            .ToListAsync(cancellationToken);

        return new GroupDetails
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            IsSystemGroup = group.IsSystemGroup,
            CreatedAt = group.CreatedAt,
            ModifiedAt = group.ModifiedAt,
            Members = members,
            Roles = roles
        };
    }

    public async Task<GroupSummary> CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        if (await db.Groups.AnyAsync(g => g.Name == request.Name, cancellationToken))
        {
            throw new InvalidOperationException("Group name already exists");
        }

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            IsSystemGroup = false,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        db.Groups.Add(group);
        await db.SaveChangesAsync(cancellationToken);

        return ToGroupSummary(group);
    }

    public async Task<GroupSummary> UpdateGroupAsync(
        Guid groupId,
        UpdateGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            throw new KeyNotFoundException("Group not found");
        }

        if (request.Name != group.Name
            && await db.Groups.AnyAsync(g => g.Name == request.Name && g.Id != groupId, cancellationToken))
        {
            throw new InvalidOperationException("Group name already exists");
        }

        group.Name = request.Name;
        group.Description = request.Description;
        group.ModifiedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return ToGroupSummary(group);
    }

    public async Task DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId, cancellationToken);
        if (group is null)
        {
            throw new KeyNotFoundException("Group not found");
        }

        if (group.IsSystemGroup)
        {
            throw new InvalidOperationException("Cannot delete system groups");
        }

        db.Groups.Remove(group);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserSummary>?> GetGroupMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        if (!await db.Groups.AnyAsync(g => g.Id == groupId, cancellationToken))
        {
            return null;
        }

        var userIds = await db.UserGroups
            .AsNoTracking()
            .Where(ug => ug.GroupId == groupId)
            .Select(ug => ug.UserId)
            .ToListAsync(cancellationToken);

        var users = await db.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .OrderBy(u => u.Username)
            .Select(u => UserService.ToUserSummary(u))
            .ToListAsync(cancellationToken);

        return users;
    }

    public async Task AddMemberAsync(
        Guid groupId,
        AddGroupMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Groups.AnyAsync(g => g.Id == groupId, cancellationToken))
        {
            throw new KeyNotFoundException("Group not found");
        }

        var exists = await db.UserGroups.AnyAsync(
            ug => ug.GroupId == groupId && ug.UserId == request.UserId,
            cancellationToken);

        if (!exists)
        {
            db.UserGroups.Add(new UserGroup
            {
                GroupId = groupId,
                UserId = request.UserId
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveMemberAsync(
        Guid groupId,
        RemoveGroupMemberRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Groups.AnyAsync(g => g.Id == groupId, cancellationToken))
        {
            throw new KeyNotFoundException("Group not found");
        }

        var row = await db.UserGroups.FirstOrDefaultAsync(
            ug => ug.GroupId == groupId && ug.UserId == request.UserId,
            cancellationToken);

        if (row is not null)
        {
            db.UserGroups.Remove(row);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SetMembersAsync(
        Guid groupId,
        SetGroupMembersRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Groups.AnyAsync(g => g.Id == groupId, cancellationToken))
        {
            throw new KeyNotFoundException("Group not found");
        }

        var existing = await db.UserGroups
            .Where(ug => ug.GroupId == groupId)
            .ToListAsync(cancellationToken);

        db.UserGroups.RemoveRange(existing);

        var rows = request.UserIds
            .Distinct()
            .Select(userId => new UserGroup
            {
                GroupId = groupId,
                UserId = userId
            });

        db.UserGroups.AddRange(rows);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoleSummary>?> GetGroupRolesAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        if (!await db.Groups.AnyAsync(g => g.Id == groupId, cancellationToken))
        {
            return null;
        }

        var roleIds = await db.GroupRoles
            .AsNoTracking()
            .Where(gr => gr.GroupId == groupId)
            .Select(gr => gr.RoleId)
            .ToListAsync(cancellationToken);

        var roles = await db.Roles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .Select(r => RoleService.ToRoleSummary(r))
            .ToListAsync(cancellationToken);

        return roles;
    }

    public async Task AssignRoleAsync(
        Guid groupId,
        AssignGroupRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Groups.AnyAsync(g => g.Id == groupId, cancellationToken))
        {
            throw new KeyNotFoundException("Group not found");
        }

        var exists = await db.GroupRoles.AnyAsync(
            gr => gr.GroupId == groupId && gr.RoleId == request.RoleId,
            cancellationToken);

        if (!exists)
        {
            db.GroupRoles.Add(new GroupRole
            {
                GroupId = groupId,
                RoleId = request.RoleId
            });
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveRoleAsync(
        Guid groupId,
        RemoveGroupRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Groups.AnyAsync(g => g.Id == groupId, cancellationToken))
        {
            throw new KeyNotFoundException("Group not found");
        }

        var row = await db.GroupRoles.FirstOrDefaultAsync(
            gr => gr.GroupId == groupId && gr.RoleId == request.RoleId,
            cancellationToken);

        if (row is not null)
        {
            db.GroupRoles.Remove(row);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SetRolesAsync(
        Guid groupId,
        SetGroupRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Groups.AnyAsync(g => g.Id == groupId, cancellationToken))
        {
            throw new KeyNotFoundException("Group not found");
        }

        var existing = await db.GroupRoles
            .Where(gr => gr.GroupId == groupId)
            .ToListAsync(cancellationToken);

        db.GroupRoles.RemoveRange(existing);

        var rows = request.RoleIds
            .Distinct()
            .Select(roleId => new GroupRole
            {
                GroupId = groupId,
                RoleId = roleId
            });

        db.GroupRoles.AddRange(rows);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExternalGroupMappingInfo>> GetExternalGroupMappingsAsync(
        CancellationToken cancellationToken = default)
    {
        var mappings = await db.ExternalGroupMappings
            .AsNoTracking()
            .OrderBy(m => m.Provider)
            .ThenBy(m => m.ExternalGroupName)
            .ToListAsync(cancellationToken);

        var groupIds = mappings.Select(m => m.GroupId).Distinct().ToList();
        var groups = await db.Groups
            .AsNoTracking()
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionaryAsync(g => g.Id, g => g.Name, cancellationToken);

        return mappings.Select(m => new ExternalGroupMappingInfo
        {
            Id = m.Id,
            Provider = m.Provider,
            ExternalGroupId = m.ExternalGroupId,
            ExternalGroupName = m.ExternalGroupName,
            GroupId = m.GroupId,
            GroupName = groups.GetValueOrDefault(m.GroupId, string.Empty),
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public async Task<ExternalGroupMappingInfo?> CreateExternalGroupMappingAsync(
        CreateExternalGroupMappingRequest request,
        CancellationToken cancellationToken = default)
    {
        var group = await db.Groups
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == request.GroupId, cancellationToken);

        if (group is null)
        {
            return null;
        }

        if (await db.ExternalGroupMappings.AnyAsync(
            m => m.Provider == request.Provider && m.ExternalGroupId == request.ExternalGroupId,
            cancellationToken))
        {
            throw new InvalidOperationException("External group mapping already exists");
        }

        var mapping = new ExternalGroupMapping
        {
            Id = Guid.NewGuid(),
            Provider = request.Provider,
            ExternalGroupId = request.ExternalGroupId,
            ExternalGroupName = request.ExternalGroupName,
            GroupId = request.GroupId,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        db.ExternalGroupMappings.Add(mapping);
        await db.SaveChangesAsync(cancellationToken);

        return new ExternalGroupMappingInfo
        {
            Id = mapping.Id,
            Provider = mapping.Provider,
            ExternalGroupId = mapping.ExternalGroupId,
            ExternalGroupName = mapping.ExternalGroupName,
            GroupId = mapping.GroupId,
            GroupName = group.Name,
            CreatedAt = mapping.CreatedAt
        };
    }

    public async Task DeleteExternalGroupMappingAsync(Guid mappingId, CancellationToken cancellationToken = default)
    {
        var mapping = await db.ExternalGroupMappings.FirstOrDefaultAsync(m => m.Id == mappingId, cancellationToken);
        if (mapping is null)
        {
            throw new KeyNotFoundException("External group mapping not found");
        }

        db.ExternalGroupMappings.Remove(mapping);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetGroupMemberCountsAsync(CancellationToken cancellationToken = default)
    {
        return await db.UserGroups
            .AsNoTracking()
            .GroupBy(ug => ug.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetGroupRoleCountsAsync(CancellationToken cancellationToken = default)
    {
        return await db.GroupRoles
            .AsNoTracking()
            .GroupBy(gr => gr.GroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.GroupId, x => x.Count, cancellationToken);
    }

    internal static GroupSummary ToGroupSummary(Group group)
    {
        return new GroupSummary
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            IsSystemGroup = group.IsSystemGroup,
            CreatedAt = group.CreatedAt,
            ModifiedAt = group.ModifiedAt
        };
    }
}
