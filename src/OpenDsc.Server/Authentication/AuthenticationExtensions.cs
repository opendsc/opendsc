// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Authentication;

/// <summary>
/// Extension methods for configuring authentication.
/// </summary>
public static class AuthenticationExtensions
{
    /// <summary>
    /// Adds authentication and authorization for the pull server.
    /// </summary>
    public static IServiceCollection AddServerAuthentication(this IServiceCollection services, IWebHostEnvironment environment)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IPersonalAccessTokenService, PersonalAccessTokenService>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();
        services.AddScoped<IClaimsTransformation, GroupClaimsTransformation>();

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.LoginPath = "/api/v1/auth/login";
                options.LogoutPath = "/api/v1/auth/logout";
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = true;
                options.Cookie.HttpOnly = true;
                // In Testing environment, allow HTTP for WebApplicationFactory tests
                options.Cookie.SecurePolicy = environment.IsEnvironment("Testing")
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;

                // Suppress redirects for API requests - return 401/403 instead
                options.Events.OnRedirectToLogin = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                };
            })
            .AddScheme<PersonalAccessTokenOptions, PersonalAccessTokenHandler>(
                PersonalAccessTokenHandler.SchemeName, null)
            .AddScheme<AuthenticationSchemeOptions, CertificateAuthHandler>(
                CertificateAuthHandler.NodeScheme, null);

        services.AddAuthorizationBuilder()
            .AddPolicy("Node", policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(CertificateAuthHandler.NodeScheme)
                .RequireRole("Node"))
            .AddPolicy(Permissions.ServerSettings_Read, policy => policy
                .RequireClaim("permission", Permissions.ServerSettings_Read))
            .AddPolicy(Permissions.ServerSettings_Write, policy => policy
                .RequireClaim("permission", Permissions.ServerSettings_Write))
            .AddPolicy(Permissions.Users_Manage, policy => policy
                .RequireClaim("permission", Permissions.Users_Manage))
            .AddPolicy(Permissions.Groups_Manage, policy => policy
                .RequireClaim("permission", Permissions.Groups_Manage))
            .AddPolicy(Permissions.Roles_Manage, policy => policy
                .RequireClaim("permission", Permissions.Roles_Manage))
            .AddPolicy(Permissions.RegistrationKeys_Manage, policy => policy
                .RequireClaim("permission", Permissions.RegistrationKeys_Manage))
            .AddPolicy(Permissions.Nodes_Read, policy => policy
                .RequireClaim("permission", Permissions.Nodes_Read))
            .AddPolicy(Permissions.Nodes_Write, policy => policy
                .RequireClaim("permission", Permissions.Nodes_Write))
            .AddPolicy(Permissions.Nodes_Delete, policy => policy
                .RequireClaim("permission", Permissions.Nodes_Delete))
            .AddPolicy(Permissions.Nodes_AssignConfiguration, policy => policy
                .RequireClaim("permission", Permissions.Nodes_AssignConfiguration))
            .AddPolicy(Permissions.Reports_Read, policy => policy
                .RequireClaim("permission", Permissions.Reports_Read))
            .AddPolicy(Permissions.Reports_ReadAll, policy => policy
                .RequireClaim("permission", Permissions.Reports_ReadAll))
            .AddPolicy(Permissions.Retention_Manage, policy => policy
                .RequireClaim("permission", Permissions.Retention_Manage))
            .AddPolicy(Permissions.Configurations_AdminOverride, policy => policy
                .RequireClaim("permission", Permissions.Configurations_AdminOverride))
            .AddPolicy(Permissions.CompositeConfigurations_AdminOverride, policy => policy
                .RequireClaim("permission", Permissions.CompositeConfigurations_AdminOverride))
            .AddPolicy(Permissions.Parameters_AdminOverride, policy => policy
                .RequireClaim("permission", Permissions.Parameters_AdminOverride))
            .AddPolicy(Permissions.Scopes_AdminOverride, policy => policy
                .RequireClaim("permission", Permissions.Scopes_AdminOverride));

        return services;
    }
}
