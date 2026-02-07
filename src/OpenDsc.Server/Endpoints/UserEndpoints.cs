// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Endpoints;

/// <summary>
/// Endpoints for user management.
/// </summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireAuthorization(Permissions.Users_Manage);

        group.MapGet("/", GetUsers)
            .WithSummary("List all users")
            .WithDescription("Returns a list of all users.");

        group.MapGet("/{id:guid}", GetUser)
            .WithSummary("Get user details")
            .WithDescription("Returns details for a specific user.");

        group.MapPost("/", CreateUser)
            .WithSummary("Create user")
            .WithDescription("Creates a new user account.");

        group.MapPut("/{id:guid}", UpdateUser)
            .WithSummary("Update user")
            .WithDescription("Updates user details.");

        group.MapDelete("/{id:guid}", DeleteUser)
            .WithSummary("Delete user")
            .WithDescription("Deletes a user account.");

        group.MapPost("/{id:guid}/reset-password", ResetPassword)
            .WithSummary("Reset password")
            .WithDescription("Resets a user's password and requires them to change it on next login.");

        group.MapPost("/{id:guid}/unlock", UnlockUser)
            .WithSummary("Unlock user")
            .WithDescription("Unlocks a locked user account.");

        group.MapGet("/{id:guid}/roles", GetUserRoles)
            .WithSummary("Get user roles")
            .WithDescription("Returns the roles assigned to a user.");

        group.MapPut("/{id:guid}/roles", SetUserRoles)
            .WithSummary("Set user roles")
            .WithDescription("Sets the roles for a user, replacing existing role assignments.");
    }

    private static async Task<Ok<List<UserDto>>> GetUsers(ServerDbContext db)
    {
        var users = await db.Users
            .OrderBy(u => u.Username)
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

    private static async Task<Results<Ok<UserDetailDto>, NotFound>> GetUser(
        Guid id,
        ServerDbContext db)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null)
        {
            return TypedResults.NotFound();
        }

        var roleIds = await db.UserRoles
            .Where(ur => ur.UserId == id)
            .Select(ur => ur.RoleId)
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

        var groupIds = await db.UserGroups
            .Where(ug => ug.UserId == id)
            .Select(ug => ug.GroupId)
            .ToListAsync();

        var groups = await db.Groups
            .Where(g => groupIds.Contains(g.Id))
            .Select(g => new GroupDto
            {
                Id = g.Id,
                Name = g.Name
            })
            .ToListAsync();

        return TypedResults.Ok(new UserDetailDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AccountType = user.AccountType.ToString(),
            IsActive = user.IsActive,
            RequirePasswordChange = user.RequirePasswordChange,
            LockoutEnd = user.LockoutEnd,
            AccessFailedCount = user.AccessFailedCount,
            CreatedAt = user.CreatedAt,
            ModifiedAt = user.ModifiedAt ?? user.CreatedAt,
            Roles = roles,
            Groups = groups
        });
    }

    private static async Task<Results<Created<UserDto>, BadRequest<string>>> CreateUser(
        [FromBody] CreateUserRequest request,
        ServerDbContext db,
        IPasswordHasher passwordHasher)
    {
        if (await db.Users.AnyAsync(u => u.Username == request.Username))
        {
            return TypedResults.BadRequest("Username already exists");
        }

        if (!Enum.TryParse<AccountType>(request.AccountType, ignoreCase: true, out var accountType))
        {
            var validValues = string.Join(", ", Enum.GetNames(typeof(AccountType)));
            return TypedResults.BadRequest($"Invalid account type '{request.AccountType}'. Valid values are: {validValues}.");
        }

        var (hash, salt) = passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            AccountType = accountType,
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true,
            RequirePasswordChange = request.RequirePasswordChange,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return TypedResults.Created($"/api/v1/users/{user.Id}", new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AccountType = user.AccountType.ToString(),
            IsActive = user.IsActive,
            RequirePasswordChange = user.RequirePasswordChange,
            LockoutEnd = user.LockoutEnd,
            CreatedAt = user.CreatedAt,
            ModifiedAt = user.ModifiedAt
        });
    }

    private static async Task<Results<Ok<UserDto>, NotFound, BadRequest<string>>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        ServerDbContext db)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null)
        {
            return TypedResults.NotFound();
        }

        if (request.Username != user.Username &&
            await db.Users.AnyAsync(u => u.Username == request.Username && u.Id != id))
        {
            return TypedResults.BadRequest("Username already exists");
        }

        user.Username = request.Username;
        user.Email = request.Email;
        user.IsActive = request.IsActive;
        user.ModifiedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return TypedResults.Ok(new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AccountType = user.AccountType.ToString(),
            IsActive = user.IsActive,
            RequirePasswordChange = user.RequirePasswordChange,
            LockoutEnd = user.LockoutEnd,
            CreatedAt = user.CreatedAt,
            ModifiedAt = user.ModifiedAt
        });
    }

    private static async Task<Results<NoContent, NotFound>> DeleteUser(
        Guid id,
        ServerDbContext db)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null)
        {
            return TypedResults.NotFound();
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound>> ResetPassword(
        Guid id,
        [FromBody] ResetPasswordRequest request,
        ServerDbContext db,
        IPasswordHasher passwordHasher)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null)
        {
            return TypedResults.NotFound();
        }

        var (hash, salt) = passwordHasher.HashPassword(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.RequirePasswordChange = true;
        user.ModifiedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok, NotFound>> UnlockUser(
        Guid id,
        ServerDbContext db)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null)
        {
            return TypedResults.NotFound();
        }

        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        user.ModifiedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync();

        return TypedResults.Ok();
    }

    private static async Task<Results<Ok<List<RoleDto>>, NotFound>> GetUserRoles(
        Guid id,
        ServerDbContext db)
    {
        if (!await db.Users.AnyAsync(u => u.Id == id))
        {
            return TypedResults.NotFound();
        }

        var roleIds = await db.UserRoles
            .Where(ur => ur.UserId == id)
            .Select(ur => ur.RoleId)
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

    private static async Task<Results<Ok, NotFound>> SetUserRoles(
        Guid id,
        [FromBody] SetRolesRequest request,
        ServerDbContext db)
    {
        if (!await db.Users.AnyAsync(u => u.Id == id))
        {
            return TypedResults.NotFound();
        }

        var existingRoles = await db.UserRoles
            .Where(ur => ur.UserId == id)
            .ToListAsync();

        db.UserRoles.RemoveRange(existingRoles);

        var newRoles = request.RoleIds.Select(roleId => new UserRole
        {
            UserId = id,
            RoleId = roleId
        });

        db.UserRoles.AddRange(newRoles);
        await db.SaveChangesAsync();

        return TypedResults.Ok();
    }
}

public sealed class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool RequirePasswordChange { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
}

public sealed class UserDetailDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool RequirePasswordChange { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ModifiedAt { get; set; }
    public List<RoleDto> Roles { get; set; } = [];
    public List<GroupDto> Groups { get; set; } = [];
}

public sealed class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AccountType { get; set; } = "User";
    public bool RequirePasswordChange { get; set; } = true;
}

public sealed class UpdateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class SetRolesRequest
{
    public Guid[] RoleIds { get; set; } = [];
}

public sealed class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystemRole { get; set; }
}

public sealed class GroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
