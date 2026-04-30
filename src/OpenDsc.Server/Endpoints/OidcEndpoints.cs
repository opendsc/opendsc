// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using OpenDsc.Server.Authentication;

namespace OpenDsc.Server.Endpoints;

/// <summary>
/// Endpoints for OpenID Connect authentication flows.
/// </summary>
public static class OidcEndpoints
{
    public static void MapOidcEndpoints(this IEndpointRouteBuilder app, IConfiguration configuration)
    {
        var oidcProviders = configuration
            .GetSection("Authentication:OidcProviders")
            .Get<OidcProviderOptions[]>() ?? [];

        var providerMap = oidcProviders.ToDictionary(
            p => p.Name,
            p => p,
            StringComparer.OrdinalIgnoreCase);

        var group = app.MapGroup("/api/v1/auth/oidc")
            .WithTags("Authentication");

        group.MapGet("/providers", () =>
        {
            var result = oidcProviders.Select(p => new OidcProviderInfo
            {
                Name = p.Name,
                DisplayName = p.DisplayName
            });
            return Results.Ok(result);
        })
        .WithSummary("List configured OIDC providers")
        .WithDescription("Returns the list of configured OpenID Connect providers available for login.")
        .AllowAnonymous();

        group.MapGet("/{provider}/challenge", (string provider, string? returnUrl, HttpContext context) =>
        {
            if (!providerMap.TryGetValue(provider, out _))
            {
                return Results.NotFound();
            }

            var redirectUri = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
            var properties = new AuthenticationProperties { RedirectUri = redirectUri };
            return Results.Challenge(properties, [AuthenticationExtensions.OidcSchemeName(provider)]);
        })
        .WithSummary("Initiate OIDC login")
        .WithDescription("Redirects the user to the specified OIDC provider for authentication.")
        .AllowAnonymous();
    }
}

/// <summary>
/// A summary of a configured OIDC provider for display on the login page.
/// </summary>
public sealed class OidcProviderInfo
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
