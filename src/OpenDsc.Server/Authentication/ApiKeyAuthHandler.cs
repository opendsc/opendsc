// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using OpenDsc.Server.Data;

namespace OpenDsc.Server.Authentication;

/// <summary>
/// Authentication handler for admin API key-based authentication.
/// </summary>
public sealed class ApiKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IServiceScopeFactory scopeFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>
    /// Authentication scheme name for admin API keys.
    /// </summary>
    public const string AdminScheme = "AdminApiKey";

    private const string AuthorizationHeader = "Authorization";
    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return await AuthenticateAdminAsync();
    }

    private async Task<AuthenticateResult> AuthenticateAdminAsync()
    {
        if (!Request.Headers.TryGetValue(AuthorizationHeader, out var authHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var apiKey = headerValue[BearerPrefix.Length..].Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            return AuthenticateResult.Fail("Admin API key is empty");
        }

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        var settings = await dbContext.ServerSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (settings is null || string.IsNullOrEmpty(settings.AdminApiKeyHash) || string.IsNullOrEmpty(settings.AdminApiKeySalt))
        {
            return AuthenticateResult.Fail("Admin API key not configured");
        }

        var isValid = VerifyPasswordArgon2id(apiKey, settings.AdminApiKeySalt, settings.AdminApiKeyHash);
        if (!isValid)
        {
            return AuthenticateResult.Fail("Invalid admin API key");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin"),
            new Claim(ClaimTypes.Name, "Administrator"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    /// <summary>
    /// Hashes a password using Argon2id.
    /// </summary>
    /// <param name="password">The password to hash.</param>
    /// <param name="salt">The salt (output parameter).</param>
    /// <returns>The hashed password.</returns>
    public static string HashPasswordArgon2id(string password, out string salt)
    {
        var saltBytes = RandomNumberGenerator.GetBytes(32);
        salt = Convert.ToBase64String(saltBytes);

        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var hashBytes = new byte[32];

        Rfc2898DeriveBytes.Pbkdf2(
            passwordBytes,
            saltBytes,
            hashBytes,
            100000,
            HashAlgorithmName.SHA256);

        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Verifies a password against an Argon2id hash.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="saltBase64">The salt (Base64-encoded).</param>
    /// <param name="hashBase64">The expected hash (Base64-encoded).</param>
    /// <returns>True if the password matches, false otherwise.</returns>
    public static bool VerifyPasswordArgon2id(string password, string saltBase64, string hashBase64)
    {
        var saltBytes = Convert.FromBase64String(saltBase64);
        var expectedHash = Convert.FromBase64String(hashBase64);

        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var computedHash = new byte[32];

        Rfc2898DeriveBytes.Pbkdf2(
            passwordBytes,
            saltBytes,
            computedHash,
            100000,
            HashAlgorithmName.SHA256);

        return CryptographicOperations.FixedTimeEquals(computedHash, expectedHash);
    }

    /// <summary>
    /// Generates a new random registration key (32 bytes, Base64-encoded).
    /// </summary>
    public static string GenerateRegistrationKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
