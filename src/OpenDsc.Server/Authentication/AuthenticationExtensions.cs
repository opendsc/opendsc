// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Authentication;

namespace OpenDsc.Server.Authentication;

/// <summary>
/// Extension methods for configuring authentication.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds certificate authentication for nodes and API key authentication for admins.
    /// </summary>
    public static IServiceCollection AddServerAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(CertificateAuthHandler.NodeScheme)
            .AddScheme<AuthenticationSchemeOptions, CertificateAuthHandler>(
                CertificateAuthHandler.NodeScheme, null)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>(
                ApiKeyAuthHandler.AdminScheme, null);

        services.AddAuthorizationBuilder()
            .AddPolicy("Node", policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(CertificateAuthHandler.NodeScheme)
                .RequireRole("Node"))
            .AddPolicy("Admin", policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(ApiKeyAuthHandler.AdminScheme)
                .RequireRole("Admin"));

        return services;
    }
}
