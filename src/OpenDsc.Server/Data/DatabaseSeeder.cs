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

        foreach (var role in roles)
        {
            if (!await db.Roles.AnyAsync(r => r.Name == role.Name))
            {
                db.Roles.Add(role);
                logger.LogInformation("Seeded role: {RoleName}", role.Name);
            }
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

        logger.LogWarning(
            "Initial admin account created with default credentials. " +
            "Username: {Username}, Password: {Password}. " +
            "CHANGE THIS PASSWORD IMMEDIATELY!",
            username,
            defaultPassword);
    }

    /// <summary>
    /// Seeds all default data.
    /// </summary>
    public static async Task SeedDefaultDataAsync(ServerDbContext db, IPasswordHasher passwordHasher, ILogger logger)
    {
        await SeedRolesAsync(db, logger);
        await SeedInitialAdminAsync(db, passwordHasher, logger);
    }
}
