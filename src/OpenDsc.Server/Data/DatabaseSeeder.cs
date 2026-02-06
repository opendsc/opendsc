// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Data;

/// <summary>
/// Database seeding extensions.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds predefined roles.
    /// </summary>
    public static async Task SeedRolesAsync(ServerDbContext db, ILogger logger)
    {
        var now = DateTimeOffset.UtcNow;

        var roles = new[]
        {
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "Administrator",
                Description = "Full server administration with access to all resources",
                IsSystemRole = true,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    Permissions.ServerSettings_Read,
                    Permissions.ServerSettings_Write,
                    Permissions.Users_Manage,
                    Permissions.Groups_Manage,
                    Permissions.Roles_Manage,
                    Permissions.RegistrationKeys_Manage,
                    Permissions.Nodes_Read,
                    Permissions.Nodes_Write,
                    Permissions.Nodes_Delete,
                    Permissions.Nodes_AssignConfiguration,
                    Permissions.Reports_Read,
                    Permissions.Reports_ReadAll,
                    Permissions.Retention_Manage,
                    Permissions.Configurations_AdminOverride,
                    Permissions.CompositeConfigurations_AdminOverride,
                    Permissions.Parameters_AdminOverride,
                    Permissions.Scopes_AdminOverride
                }),
                CreatedAt = now
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "Operator",
                Description = "Manage nodes and assign configurations. Configuration access via ACLs.",
                IsSystemRole = true,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    Permissions.Nodes_Read,
                    Permissions.Nodes_Write,
                    Permissions.Nodes_AssignConfiguration,
                    Permissions.Reports_Read,
                    Permissions.ServerSettings_Read
                }),
                CreatedAt = now
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "Viewer",
                Description = "Read-only access to server info. Resource access via ACLs.",
                IsSystemRole = true,
                Permissions = JsonSerializer.Serialize(new[]
                {
                    Permissions.ServerSettings_Read,
                    Permissions.Reports_Read
                }),
                CreatedAt = now
            }
        };

        var roleNames = roles.Select(r => r.Name).ToArray();
        var existingRoleNames = await db.Roles
            .Where(r => roleNames.Contains(r.Name))
            .Select(r => r.Name)
            .ToListAsync();

        var missingRoles = roles.Where(r => !existingRoleNames.Contains(r.Name));

        foreach (var role in missingRoles)
        {
            db.Roles.Add(role);
            logger.LogInformation("Seeded role: {RoleName}", role.Name);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds initial admin user with default credentials (admin/admin).
    /// </summary>
    public static async Task SeedInitialAdminAsync(ServerDbContext db, IPasswordHasher passwordHasher, ILogger logger)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        const string username = "admin";
        const string defaultPassword = "admin";

        var (hash, salt) = passwordHasher.HashPassword(defaultPassword);

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = "admin@localhost",
            PasswordHash = hash,
            PasswordSalt = salt,
            AccountType = AccountType.User,
            EmailConfirmed = true,
            RequirePasswordChange = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.Users.Add(adminUser);
        await db.SaveChangesAsync();

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
        if (adminRole != null)
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id
            });
            await db.SaveChangesAsync();
        }

        // Add admin user to Administrators group
        var adminGroup = await db.Groups.FirstOrDefaultAsync(g => g.Name == "Administrators");
        if (adminGroup != null)
        {
            db.UserGroups.Add(new UserGroup
            {
                UserId = adminUser.Id,
                GroupId = adminGroup.Id
            });
            await db.SaveChangesAsync();
        }

        // Assign Administrator role to Administrators group
        if (adminRole != null && adminGroup != null)
        {
            db.GroupRoles.Add(new GroupRole
            {
                GroupId = adminGroup.Id,
                RoleId = adminRole.Id
            });
            await db.SaveChangesAsync();
        }

        logger.LogWarning(
            "Initial admin account created with default credentials. " +
            "CHANGE THIS PASSWORD IMMEDIATELY!");
    }
    public static async Task SeedDefaultGroupsAsync(ServerDbContext db, ILogger logger)
    {
        var now = DateTimeOffset.UtcNow;

        var groups = new[]
        {
            new Group
            {
                Id = Guid.NewGuid(),
                Name = "Administrators",
                Description = "Default group for administrators",
                CreatedAt = now
            },
            new Group
            {
                Id = Guid.NewGuid(),
                Name = "Operators",
                Description = "Default group for operators",
                CreatedAt = now
            }
        };

        var groupNames = groups.Select(g => g.Name).ToArray();
        var existingGroupNames = await db.Groups
            .Where(g => groupNames.Contains(g.Name))
            .Select(g => g.Name)
            .ToListAsync();

        var missingGroups = groups.Where(g => !existingGroupNames.Contains(g.Name));

        foreach (var group in missingGroups)
        {
            db.Groups.Add(group);
            logger.LogInformation("Seeded group: {GroupName}", group.Name);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds system scope types.
    /// </summary>
    public static async Task SeedSystemScopeTypesAsync(ServerDbContext db, ILogger logger)
    {
        var now = DateTimeOffset.UtcNow;

        var scopeTypes = new[]
        {
            new ScopeType
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), // Fixed ID for Default scope
                Name = "Default",
                Description = "Default scope type for global parameters",
                Precedence = 0,
                IsSystem = true,
                AllowsValues = false,
                CreatedAt = now
            },
            new ScopeType
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), // Fixed ID for Node scope
                Name = "Node",
                Description = "Node-specific scope type for per-node parameters",
                Precedence = 100,
                IsSystem = true,
                AllowsValues = false,
                CreatedAt = now
            }
        };

        var scopeTypeNames = scopeTypes.Select(st => st.Name).ToArray();
        var existingScopeTypeNames = await db.ScopeTypes
            .Where(st => scopeTypeNames.Contains(st.Name))
            .Select(st => st.Name)
            .ToListAsync();

        var missingScopeTypes = scopeTypes.Where(st => !existingScopeTypeNames.Contains(st.Name));

        foreach (var scopeType in missingScopeTypes)
        {
            db.ScopeTypes.Add(scopeType);
            logger.LogInformation("Seeded system scope type: {ScopeTypeName}", scopeType.Name);
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds all default data.
    /// </summary>
    public static async Task SeedDefaultDataAsync(ServerDbContext db, IPasswordHasher passwordHasher, ILogger logger)
    {
        await SeedRolesAsync(db, logger);
        await SeedDefaultGroupsAsync(db, logger);
        await SeedSystemScopeTypesAsync(db, logger);
        await SeedInitialAdminAsync(db, passwordHasher, logger);
    }
}
