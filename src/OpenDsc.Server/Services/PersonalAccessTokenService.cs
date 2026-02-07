// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography;
using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

/// <summary>
/// Service for creating and validating Personal Access Tokens.
/// </summary>
public interface IPersonalAccessTokenService
{
    /// <summary>
    /// Creates a new PAT for a user.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="name">Friendly name for the token.</param>
    /// <param name="scopes">Permissions scoped to this token.</param>
    /// <param name="expiresAt">Optional expiration date.</param>
    /// <returns>Plaintext token (shown only once) and token metadata.</returns>
    Task<(string Token, PersonalAccessToken Metadata)> CreateTokenAsync(
        Guid userId,
        string name,
        string[] scopes,
        DateTimeOffset? expiresAt);

    /// <summary>
    /// Validates a token and returns the associated token ID, user ID and scopes.
    /// </summary>
    /// <param name="token">Plaintext token to validate.</param>
    /// <returns>Token ID, User ID and scopes if valid, null otherwise.</returns>
    Task<(Guid TokenId, Guid UserId, string[] Scopes)?> ValidateTokenAsync(string token);

    /// <summary>
    /// Revokes a token.
    /// </summary>
    /// <param name="tokenId">Token ID to revoke.</param>
    Task RevokeTokenAsync(Guid tokenId);

    /// <summary>
    /// Gets all tokens for a user (metadata only, no plaintext).
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>List of token metadata.</returns>
    Task<List<PersonalAccessToken>> GetUserTokensAsync(Guid userId);

    /// <summary>
    /// Updates last used timestamp and IP address asynchronously.
    /// </summary>
    /// <param name="tokenId">Token ID.</param>
    /// <param name="ipAddress">IP address.</param>
    Task UpdateLastUsedAsync(Guid tokenId, string ipAddress);
}

/// <summary>
/// Personal Access Token service implementation.
/// </summary>
public class PersonalAccessTokenService(
    ServerDbContext db,
    IPasswordHasher passwordHasher) : IPersonalAccessTokenService
{
    private const int TokenBodyLength = 40;
    private const string TokenPrefix = "pat_";

    public async Task<(string Token, PersonalAccessToken Metadata)> CreateTokenAsync(
        Guid userId,
        string name,
        string[] scopes,
        DateTimeOffset? expiresAt)
    {
        // Generate enough bytes to ensure we have TokenBodyLength characters after removing special chars
        // Base64 encoding: 3 bytes = 4 characters, but we remove +, /, and =
        // Generate extra bytes to account for removed characters
        var tokenBytes = RandomNumberGenerator.GetBytes(TokenBodyLength);
        var tokenBodyBuilder = new System.Text.StringBuilder(
            Convert.ToBase64String(tokenBytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", ""));

        // Ensure we have exactly TokenBodyLength characters
        while (tokenBodyBuilder.Length < TokenBodyLength)
        {
            var extraBytes = RandomNumberGenerator.GetBytes(TokenBodyLength - tokenBodyBuilder.Length);
            tokenBodyBuilder.Append(Convert.ToBase64String(extraBytes)
                .Replace("+", "")
                .Replace("/", "")
                .Replace("=", ""));
        }

        var tokenBody = tokenBodyBuilder.ToString()[..TokenBodyLength];
        var token = TokenPrefix + tokenBody;
        var (hash, salt) = passwordHasher.HashPassword(token);

        var patEntity = new PersonalAccessToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = name,
            TokenHash = hash + ":" + salt,
            TokenPrefix = token[..Math.Min(8, token.Length)],
            Scopes = JsonSerializer.Serialize(scopes),
            ExpiresAt = expiresAt,
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.PersonalAccessTokens.Add(patEntity);
        await db.SaveChangesAsync();

        return (token, patEntity);
    }

    public async Task<(Guid TokenId, Guid UserId, string[] Scopes)?> ValidateTokenAsync(string token)
    {
        if (!token.StartsWith(TokenPrefix))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var tokenPrefixValue = token[..Math.Min(8, token.Length)];

        var tokens = await db.PersonalAccessTokens
            .Where(t => !t.IsRevoked && t.TokenPrefix == tokenPrefixValue)
            .ToListAsync();

        foreach (var pat in tokens)
        {
            // Check expiration
            if (pat.ExpiresAt.HasValue && pat.ExpiresAt.Value <= now)
            {
                continue;
            }

            var parts = pat.TokenHash.Split(':');
            if (parts.Length != 2)
            {
                continue;
            }

            if (passwordHasher.ValidatePassword(token, parts[0], parts[1]))
            {
                var scopes = JsonSerializer.Deserialize<string[]>(pat.Scopes) ?? [];
                return (pat.Id, pat.UserId, scopes);
            }
        }

        return null;
    }

    public async Task RevokeTokenAsync(Guid tokenId)
    {
        var token = await db.PersonalAccessTokens.FindAsync(tokenId);
        if (token != null)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<PersonalAccessToken>> GetUserTokensAsync(Guid userId)
    {
        return (await db.PersonalAccessTokens
            .Where(t => t.UserId == userId)
            .ToListAsync())
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
    }

    public async Task UpdateLastUsedAsync(Guid tokenId, string ipAddress)
    {
        var token = await db.PersonalAccessTokens.FindAsync(tokenId);
        if (token != null)
        {
            token.LastUsedAt = DateTimeOffset.UtcNow;
            token.LastUsedIpAddress = ipAddress;
            await db.SaveChangesAsync();
        }
    }
}
