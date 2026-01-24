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
/// Authentication handler for API key-based authentication.
/// </summary>
public sealed class ApiKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IServiceScopeFactory scopeFactory)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <summary>
    /// Authentication scheme name for node API keys.
    /// </summary>
    public const string NodeScheme = "NodeApiKey";

    /// <summary>
    /// Authentication scheme name for admin API keys.
    /// </summary>
    public const string AdminScheme = "AdminApiKey";

    private const string AuthorizationHeader = "Authorization";
    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Scheme.Name == AdminScheme)
        {
            return await AuthenticateAdminAsync();
        }

        return await AuthenticateNodeAsync();
    }

    private async Task<AuthenticateResult> AuthenticateNodeAsync()
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
            return AuthenticateResult.Fail("API key is empty");
        }

        var keyHash = HashApiKey(apiKey);

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        var node = await dbContext.Nodes
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.ApiKeyHash == keyHash);

        if (node is null)
        {
            return AuthenticateResult.Fail("Invalid API key");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, node.Id.ToString()),
            new Claim(ClaimTypes.Name, node.Fqdn),
            new Claim("node_id", node.Id.ToString()),
            new Claim(ClaimTypes.Role, "Node")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
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

        var keyHash = HashApiKey(apiKey);

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        var settings = await dbContext.ServerSettings
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (settings is null || settings.AdminApiKeyHash != keyHash)
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
    /// Hashes an API key using SHA256.
    /// </summary>
    public static string HashApiKey(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Generates a new random API key.
    /// </summary>
    public static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
