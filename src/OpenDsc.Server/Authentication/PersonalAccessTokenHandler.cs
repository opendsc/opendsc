// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

using OpenDsc.Server.Data;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Authentication;

/// <summary>
/// Options for Personal Access Token authentication.
/// </summary>
public class PersonalAccessTokenOptions : AuthenticationSchemeOptions
{
}

/// <summary>
/// Authentication handler for Personal Access Token authentication.
/// </summary>
public sealed partial class PersonalAccessTokenHandler(
    IOptionsMonitor<PersonalAccessTokenOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    IServiceScopeFactory scopeFactory,
    ILogger<PersonalAccessTokenHandler> logger)
    : AuthenticationHandler<PersonalAccessTokenOptions>(options, loggerFactory, encoder)
{
    /// <summary>
    /// Authentication scheme name for Personal Access Tokens.
    /// </summary>
    public const string SchemeName = "PersonalAccessToken";

    private const string AuthorizationHeader = "Authorization";
    private const string BearerPrefix = "Bearer ";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
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

        var token = headerValue[BearerPrefix.Length..].Trim();

        if (!token.StartsWith("pat_"))
        {
            return AuthenticateResult.NoResult();
        }

        using var scope = scopeFactory.CreateScope();
        var patService = scope.ServiceProvider.GetRequiredService<IPersonalAccessTokenService>();
        var userContextService = scope.ServiceProvider.GetRequiredService<IUserContextService>();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        var result = await patService.ValidateTokenAsync(token);
        if (result == null)
        {
            LogPatHandlerInvalidToken();
            return AuthenticateResult.Fail("Invalid or expired token");
        }

        var (tokenId, userId, scopes) = result.Value;
        var userPermissions = await RbacPermissionResolver.ResolveUserAndInternalGroupPermissionsAsync(db, userId);
        var effectivePermissions = scopes
            .Where(userPermissions.Contains)
            .Distinct(StringComparer.Ordinal);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, $"PAT User {userId}"),
            new(ClaimTypes.AuthenticationMethod, SchemeName)
        };

        foreach (var scopeItem in scopes)
        {
            claims.Add(new Claim("scope", scopeItem));
        }

        foreach (var permission in effectivePermissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        var ipAddress = userContextService.GetIpAddress() ?? "unknown";
        _ = Task.Run(async () =>
        {
            using var updateScope = scopeFactory.CreateScope();
            var updatePatService = updateScope.ServiceProvider.GetRequiredService<IPersonalAccessTokenService>();

            await updatePatService.UpdateLastUsedAsync(tokenId, ipAddress);
        });

        LogPatHandlerSuccess(userId);
        return AuthenticateResult.Success(ticket);
    }

    [LoggerMessage(EventId = EventIds.PatHandlerInvalidToken, Level = LogLevel.Warning, Message = "Personal access token validation failed: invalid or expired token")]
    private partial void LogPatHandlerInvalidToken();

    [LoggerMessage(EventId = EventIds.PatHandlerSuccess, Level = LogLevel.Debug, Message = "Personal access token authentication succeeded for user {UserId}")]
    private partial void LogPatHandlerSuccess(Guid userId);
}
