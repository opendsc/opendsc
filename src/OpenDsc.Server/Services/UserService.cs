// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Contracts.Users;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

public sealed class UserService(
    ServerDbContext db,
    IPasswordHasher passwordHasher) : IUserService
{
    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await db.Users
            .AsNoTracking()
            .OrderBy(u => u.Username)
            .ToListAsync(cancellationToken);

        return users.Select(ToUserSummary).ToList();
    }

    public async Task<UserDetails> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException("User not found");
        }

        var roleIds = await db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        var roles = await db.Roles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .Select(r => RoleService.ToRoleSummary(r))
            .ToListAsync(cancellationToken);

        var groupIds = await db.UserGroups
            .AsNoTracking()
            .Where(ug => ug.UserId == userId)
            .Select(ug => ug.GroupId)
            .ToListAsync(cancellationToken);

        var groups = await db.Groups
            .AsNoTracking()
            .Where(g => groupIds.Contains(g.Id))
            .OrderBy(g => g.Name)
            .Select(g => GroupService.ToGroupSummary(g))
            .ToListAsync(cancellationToken);

        return new UserDetails
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AccountType = user.AccountType,
            IsActive = user.IsActive,
            RequirePasswordChange = user.RequirePasswordChange,
            LockoutEnd = user.LockoutEnd,
            AccessFailedCount = user.AccessFailedCount,
            Description = user.Description,
            CreatedAt = user.CreatedAt,
            ModifiedAt = user.ModifiedAt,
            Roles = roles,
            Groups = groups
        };
    }

    public async Task<UserSummary> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(u => u.Username == request.Username, cancellationToken))
        {
            throw new InvalidOperationException("Username already exists");
        }

        if (await db.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists");
        }

        var (hash, salt) = passwordHasher.HashPassword(request.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            Email = request.Email,
            AccountType = request.AccountType,
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true,
            RequirePasswordChange = request.RequirePasswordChange,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return ToUserSummary(user);
    }

    public async Task<UserSummary> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("User not found");
        }

        if (request.Username != user.Username
            && await db.Users.AnyAsync(u => u.Username == request.Username && u.Id != userId, cancellationToken))
        {
            throw new InvalidOperationException("Username already exists");
        }

        if (request.Email != user.Email
            && await db.Users.AnyAsync(u => u.Email == request.Email && u.Id != userId, cancellationToken))
        {
            throw new InvalidOperationException("Email already exists");
        }

        user.Username = request.Username;
        user.Email = request.Email;
        user.IsActive = request.IsActive;
        user.RequirePasswordChange = request.RequirePasswordChange;
        user.AccountType = request.AccountType;
        user.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description;
        user.LockoutEnd = request.IsLocked ? DateTimeOffset.UtcNow.AddYears(100) : null;
        user.ModifiedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return ToUserSummary(user);
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("User not found");
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ResetPasswordAsync(
        Guid userId,
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("User not found");
        }

        var (hash, salt) = passwordHasher.HashPassword(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.RequirePasswordChange = true;
        user.ModifiedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("User not found");
        }

        user.LockoutEnd = null;
        user.AccessFailedCount = 0;
        user.ModifiedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoleSummary>?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, cancellationToken))
        {
            return null;
        }

        var roleIds = await db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        var roles = await db.Roles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .OrderBy(r => r.Name)
            .Select(r => RoleService.ToRoleSummary(r))
            .ToListAsync(cancellationToken);

        return roles;
    }

    public async Task SetUserRolesAsync(
        Guid userId,
        SetUserRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, cancellationToken))
        {
            throw new KeyNotFoundException("User not found");
        }

        var existing = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .ToListAsync(cancellationToken);

        db.UserRoles.RemoveRange(existing);

        var roleIds = request.RoleIds.Distinct().ToArray();
        var newRoles = roleIds.Select(roleId => new UserRole
        {
            UserId = userId,
            RoleId = roleId
        });

        db.UserRoles.AddRange(newRoles);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task AssignRoleAsync(
        Guid userId,
        AssignRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, cancellationToken))
        {
            throw new KeyNotFoundException("User not found");
        }

        var exists = await db.UserRoles.AnyAsync(
            ur => ur.UserId == userId && ur.RoleId == request.RoleId,
            cancellationToken);

        if (!exists)
        {
            db.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = request.RoleId
            });

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task RemoveRoleAsync(
        Guid userId,
        RemoveRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await db.Users.AnyAsync(u => u.Id == userId, cancellationToken))
        {
            throw new KeyNotFoundException("User not found");
        }

        var entity = await db.UserRoles.FirstOrDefaultAsync(
            ur => ur.UserId == userId && ur.RoleId == request.RoleId,
            cancellationToken);

        if (entity is not null)
        {
            db.UserRoles.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<AuthenticationResult> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive, cancellationToken);

        if (user is null || user.PasswordHash is null || user.PasswordSalt is null)
        {
            return new AuthenticationResult { IsAuthenticated = false };
        }

        if (user.AccountType == AccountType.ServiceAccount)
        {
            return new AuthenticationResult { IsAuthenticated = false };
        }

        if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow)
        {
            return new AuthenticationResult { IsAuthenticated = false, IsLockedOut = true };
        }

        if (!passwordHasher.ValidatePassword(password, user.PasswordHash, user.PasswordSalt))
        {
            user.AccessFailedCount++;
            if (user.AccessFailedCount >= 5)
            {
                user.LockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
            }

            await db.SaveChangesAsync(cancellationToken);
            return new AuthenticationResult { IsAuthenticated = false, IsLockedOut = user.LockoutEnd.HasValue };
        }

        user.AccessFailedCount = 0;
        user.LockoutEnd = null;
        await db.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult
        {
            IsAuthenticated = true,
            User = ToUserSummary(user)
        };
    }

    public async Task<CurrentUserDetails?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roleIds = await db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync(cancellationToken);

        var roleNames = await db.Roles
            .AsNoTracking()
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        var authProvider = await db.ExternalLogins
            .AsNoTracking()
            .Where(el => el.UserId == userId)
            .Select(el => el.Provider)
            .FirstOrDefaultAsync(cancellationToken);

        return new CurrentUserDetails
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            AccountType = user.AccountType,
            Roles = roleNames,
            AuthProvider = authProvider
        };
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            throw new KeyNotFoundException("User not found");
        }

        if (user.PasswordHash is null || user.PasswordSalt is null)
        {
            throw new InvalidOperationException("Password changes are not available for this account");
        }

        if (!passwordHasher.ValidatePassword(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
        {
            throw new InvalidOperationException("Current password is incorrect");
        }

        var (newHash, newSalt) = passwordHasher.HashPassword(request.NewPassword);
        user.PasswordHash = newHash;
        user.PasswordSalt = newSalt;
        user.RequirePasswordChange = false;
        user.ModifiedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> GetExternalLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await db.ExternalLogins
            .AsNoTracking()
            .Where(el => el.UserId == userId)
            .Select(el => el.Provider)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetUserRoleCountsAsync(CancellationToken cancellationToken = default)
    {
        return await db.UserRoles
            .AsNoTracking()
            .GroupBy(ur => ur.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetUserGroupCountsAsync(CancellationToken cancellationToken = default)
    {
        return await db.UserGroups
            .AsNoTracking()
            .GroupBy(ug => ug.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);
    }

    public Task<HashSet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return RbacPermissionResolver.ResolveUserAndInternalGroupPermissionsAsync(db, userId);
    }

    internal static UserSummary ToUserSummary(User user)
    {
        return new UserSummary
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AccountType = user.AccountType,
            IsActive = user.IsActive,
            RequirePasswordChange = user.RequirePasswordChange,
            LockoutEnd = user.LockoutEnd,
            CreatedAt = user.CreatedAt,
            ModifiedAt = user.ModifiedAt
        };
    }
}
