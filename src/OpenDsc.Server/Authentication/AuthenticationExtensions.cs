// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Authentication;

namespace OpenDsc.Server.Authentication;

/// <summary>
/// Extension methods for configuring API key authentication.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds API key authentication for nodes and admins.
    /// </summary>
    public static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(ApiKeyAuthHandler.NodeScheme)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>(
                ApiKeyAuthHandler.NodeScheme, null)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>(
                ApiKeyAuthHandler.AdminScheme, null);

        services.AddAuthorizationBuilder()
            .AddPolicy("Node", policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(ApiKeyAuthHandler.NodeScheme)
                .RequireRole("Node"))
            .AddPolicy("Admin", policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(ApiKeyAuthHandler.AdminScheme)
                .RequireRole("Admin"));

        return services;
    }
}
