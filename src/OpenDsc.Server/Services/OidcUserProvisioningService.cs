// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

/// <summary>
/// Provisions or retrieves a user for an external OIDC login.
/// </summary>
public sealed partial class OidcUserProvisioningService(
    ServerDbContext db,
    ILogger<OidcUserProvisioningService> logger) : IOidcUserProvisioningService
{
    public async Task<User> ProvisionOrGetUserAsync(
        string provider,
        string providerKey,
        string? displayName,
        string? email,
        string? preferredUsername,
        CancellationToken cancellationToken = default)
    {
        var existingLogin = await db.ExternalLogins
            .Where(el => el.Provider == provider && el.ProviderKey == providerKey)
            .Join(db.Users, el => el.UserId, u => u.Id, (el, u) => u)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingLogin is not null)
        {
            return existingLogin;
        }

        var username = await GenerateUniqueUsernameAsync(preferredUsername, email, cancellationToken);
        var resolvedEmail = email ?? $"{username}@external";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = resolvedEmail,
            IsActive = true,
            RequirePasswordChange = false,
            AccountType = AccountType.User,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        var externalLogin = new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = provider,
            ProviderKey = providerKey,
            ProviderDisplayName = displayName,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Users.Add(user);
        db.ExternalLogins.Add(externalLogin);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        LogUserProvisioned(user.Id, user.Username, provider);
        return user;
    }

    private async Task<string> GenerateUniqueUsernameAsync(
        string? preferredUsername,
        string? email,
        CancellationToken cancellationToken)
    {
        var baseUsername = !string.IsNullOrWhiteSpace(preferredUsername)
            ? SanitizeUsername(preferredUsername)
            : !string.IsNullOrWhiteSpace(email)
                ? SanitizeUsername(email.Split('@')[0])
                : "user";

        if (baseUsername.Length > 90)
        {
            baseUsername = baseUsername[..90];
        }

        var candidate = baseUsername;
        var suffix = 1;

        while (await db.Users.AnyAsync(u => u.Username == candidate, cancellationToken))
        {
            candidate = $"{baseUsername}{suffix}";
            suffix++;
        }

        return candidate;
    }

    private static string SanitizeUsername(string input)
    {
        var sanitized = new System.Text.StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
            {
                sanitized.Append(c);
            }
        }

        return sanitized.Length > 0 ? sanitized.ToString() : "user";
    }

    [LoggerMessage(EventId = EventIds.OidcUserProvisioned, Level = LogLevel.Information,
        Message = "Provisioned new OIDC user {UserId} with username '{Username}' for provider '{Provider}'")]
    private partial void LogUserProvisioned(Guid userId, string username, string provider);
}
