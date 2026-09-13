// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using OpenDsc.Contracts.Users;
using OpenDsc.Server.Authorization;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Endpoints;

/// <summary>
/// Endpoints for authentication operations.
/// </summary>
public static class AuthenticationEndpoints
{
    public static void MapAuthenticationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Authentication");

        group.MapPost("/login", Login)
            .WithSummary("Login with username and password")
            .WithDescription("Authenticates a user and creates a session cookie.")
            .AllowAnonymous();

        group.MapPost("/logout", (Delegate)Logout)
            .WithSummary("Logout")
            .WithDescription("Ends the current session.")
            .RequireAuthorization();

        group.MapGet("/logout-redirect", (Delegate)LogoutRedirect)
            .WithSummary("Logout and redirect")
            .WithDescription("Signs out the current session and redirects to the login page.")
            .AllowAnonymous();

        group.MapGet("/me", GetCurrentUser)
            .WithSummary("Get current user info")
            .WithDescription("Returns information about the authenticated user.")
            .RequireAuthorization();

        group.MapPost("/change-password", ChangePassword)
            .WithSummary("Change password")
            .WithDescription("Changes the current user's password.")
            .RequireAuthorization();

        group.MapPost("/tokens", CreateToken)
            .WithSummary("Create Personal Access Token")
            .WithDescription("Creates a new PAT for API authentication.")
            .RequireAuthorization();

        group.MapGet("/tokens", GetTokens)
            .WithSummary("List Personal Access Tokens")
            .WithDescription("Lists all PATs for the current user.")
            .RequireAuthorization();

        group.MapDelete("/tokens/{id}", RevokeToken)
            .WithSummary("Revoke Personal Access Token")
            .WithDescription("Revokes a PAT.")
            .RequireAuthorization();
    }

    private static async Task<Results<Ok<LoginResult>, UnauthorizedHttpResult>> Login(
        [FromBody] LoginRequest request,
        IUserService userService,
        HttpContext httpContext)
    {
        var auth = await userService.AuthenticateAsync(request.Username, request.Password);
        if (!auth.IsAuthenticated || auth.User is null)
        {
            return TypedResults.Unauthorized();
        }

        var user = auth.User;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        return TypedResults.Ok(new LoginResult
        {
            UserId = user.Id,
            Username = user.Username,
            Email = user.Email,
            RequirePasswordChange = user.RequirePasswordChange
        });
    }

    private static async Task<NoContent> Logout(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return TypedResults.NoContent();
    }

    private static async Task<IResult> LogoutRedirect(HttpContext httpContext)
    {
        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    }

    private static async Task<Results<Ok<CurrentUserDetails>, UnauthorizedHttpResult>> GetCurrentUser(
        IUserService userService,
        IUserContextService userContext)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null)
        {
            return TypedResults.Unauthorized();
        }

        var user = await userService.GetCurrentUserAsync(userId.Value);
        if (user is null)
        {
            return TypedResults.Unauthorized();
        }

        return TypedResults.Ok(user);
    }

    private static async Task<Results<NoContent, BadRequest<string>, UnauthorizedHttpResult>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        IUserService userService,
        IUserContextService userContext)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null)
        {
            return TypedResults.Unauthorized();
        }

        try
        {
            await userService.ChangePasswordAsync(userId.Value, request);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.Unauthorized();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }

        return TypedResults.NoContent();
    }

    private static async Task<Results<Created<TokenCreationResult>, ValidationProblem, UnauthorizedHttpResult>> CreateToken(
        [FromBody] CreateTokenRequest request,
        IPersonalAccessTokenService patService,
        IUserContextService userContext)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null)
        {
            return TypedResults.Unauthorized();
        }

        var scopes = (request.Scopes ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var invalidScopes = scopes
            .Where(s => !Permissions.AllScopes.Contains(s))
            .ToArray();

        if (invalidScopes.Length > 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["scopes"] = [$"Invalid scopes: {string.Join(", ", invalidScopes)}"]
            });
        }

        var (token, metadata) = await patService.CreateTokenAsync(
            userId.Value,
            request.Name,
            scopes,
            request.ExpiresAt);

        return TypedResults.Created($"/api/v1/auth/tokens/{metadata.Id}", new TokenCreationResult
        {
            Token = token,
            TokenId = metadata.Id,
            Name = metadata.Name,
            TokenPrefix = metadata.TokenPrefix,
            Scopes = scopes,
            ExpiresAt = metadata.ExpiresAt,
            CreatedAt = metadata.CreatedAt
        });
    }

    private static async Task<Results<Ok<List<TokenMetadata>>, UnauthorizedHttpResult>> GetTokens(
        IPersonalAccessTokenService patService,
        IUserContextService userContext)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null)
        {
            return TypedResults.Unauthorized();
        }

        var tokens = await patService.GetUserTokensAsync(userId.Value);

        return TypedResults.Ok(tokens);
    }

    private static async Task<Results<NoContent, UnauthorizedHttpResult, NotFound>> RevokeToken(
        Guid id,
        IPersonalAccessTokenService patService,
        IUserContextService userContext)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null)
        {
            return TypedResults.Unauthorized();
        }

        var userTokens = await patService.GetUserTokensAsync(userId.Value);
        if (!userTokens.Any(t => t.Id == id))
        {
            return TypedResults.NotFound();
        }

        await patService.RevokeTokenAsync(id);

        return TypedResults.NoContent();
    }
}
