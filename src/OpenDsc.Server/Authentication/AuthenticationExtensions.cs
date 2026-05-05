// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;

using JwtBearerTokenValidatedContext = Microsoft.AspNetCore.Authentication.JwtBearer.TokenValidatedContext;
using OpenIdConnectTokenValidatedContext = Microsoft.AspNetCore.Authentication.OpenIdConnect.TokenValidatedContext;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Authentication;

/// <summary>
/// Extension methods for configuring authentication.
/// </summary>
public static class AuthenticationExtensions
{
    internal const string UserApiBearerScheme = "UserApiBearer";

    /// <summary>
    /// Adds authentication and authorization for the pull server.
    /// </summary>
    public static IServiceCollection AddServerAuthentication(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IPersonalAccessTokenService, PersonalAccessTokenService>();
        services.AddScoped<IUserContextService, UserContextService>();
        services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();
        services.AddScoped<IClaimsTransformation, GroupClaimsTransformation>();
        services.AddScoped<IOidcUserProvisioningService, OidcUserProvisioningService>();
        services.AddSingleton<IPatUpdateQueue, PatUpdateQueue>();
        services.AddHostedService<PatUpdateBackgroundService>();

        var oidcProviders = configuration
            .GetSection("Authentication:OidcProviders")
            .Get<OidcProviderOptions[]>() ?? [];

        var authBuilder = services.AddAuthentication(options =>
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
                options.Cookie.SecurePolicy = environment.IsEnvironment("Testing")
                    ? CookieSecurePolicy.SameAsRequest
                    : CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;

                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    }
                    else
                    {
                        context.Response.Redirect($"/login?returnUrl={Uri.EscapeDataString(context.Request.Path + context.Request.QueryString)}");
                    }
                    return Task.CompletedTask;
                };
                options.Events.OnRedirectToAccessDenied = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    }
                    else
                    {
                        context.Response.Redirect("/access-denied");
                    }
                    return Task.CompletedTask;
                };
            })
            .AddScheme<PersonalAccessTokenOptions, PersonalAccessTokenHandler>(
                PersonalAccessTokenHandler.SchemeName, null)
            .AddScheme<AuthenticationSchemeOptions, CertificateAuthHandler>(
                CertificateAuthHandler.NodeScheme, null);

        foreach (var provider in oidcProviders)
        {
            var oidcSchemeName = OidcSchemeName(provider.Name);
            var jwtSchemeName = JwtSchemeName(provider.Name);

            authBuilder.AddOpenIdConnect(oidcSchemeName, options =>
            {
                options.Authority = provider.Authority;
                options.ClientId = provider.ClientId;
                options.ClientSecret = provider.ClientSecret;
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.CallbackPath = $"/signin-oidc-{provider.Name}";
                options.SignedOutCallbackPath = $"/signout-callback-oidc-{provider.Name}";
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                foreach (var scope in provider.Scopes)
                {
                    if (!options.Scope.Contains(scope))
                    {
                        options.Scope.Add(scope);
                    }
                }

                if (provider.GroupClaimType != "groups")
                {
                    options.ClaimActions.MapJsonKey("groups", provider.GroupClaimType);
                }

                options.Events.OnTokenValidated = context =>
                {
                    var capturedProviderName = provider.Name;
                    var provisioningService = context.HttpContext.RequestServices
                        .GetRequiredService<IOidcUserProvisioningService>();
                    return HandleOidcTokenValidatedAsync(context, capturedProviderName, provisioningService);
                };
            });

            authBuilder.AddJwtBearer(jwtSchemeName, options =>
            {
                options.Authority = provider.Authority;
                options.Audience = provider.ClientId;
                options.MapInboundClaims = false;

                var capturedProviderName = provider.Name;
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context => HandleJwtTokenValidatedAsync(context, capturedProviderName)
                };
            });
        }

        if (oidcProviders.Length > 0)
        {
            var providerAuthorities = oidcProviders
                .ToDictionary(p => p.Authority.TrimEnd('/'), p => JwtSchemeName(p.Name),
                    StringComparer.OrdinalIgnoreCase);

            authBuilder.AddPolicyScheme(UserApiBearerScheme, UserApiBearerScheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();

                    if (authHeader.StartsWith("Bearer pat_", StringComparison.OrdinalIgnoreCase))
                    {
                        return PersonalAccessTokenHandler.SchemeName;
                    }

                    if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        var token = authHeader["Bearer ".Length..].Trim();
                        var issuer = TryGetJwtIssuer(token);

                        if (issuer is not null)
                        {
                            var normalizedIssuer = issuer.TrimEnd('/');
                            if (providerAuthorities.TryGetValue(normalizedIssuer, out var jwtScheme))
                            {
                                return jwtScheme;
                            }
                        }
                    }

                    return CookieAuthenticationDefaults.AuthenticationScheme;
                };
            });
        }
        else
        {
            authBuilder.AddPolicyScheme(UserApiBearerScheme, UserApiBearerScheme, options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var authHeader = context.Request.Headers.Authorization.ToString();
                    return authHeader.StartsWith("Bearer pat_", StringComparison.OrdinalIgnoreCase)
                        ? PersonalAccessTokenHandler.SchemeName
                        : CookieAuthenticationDefaults.AuthenticationScheme;
                };
            });
        }

        services.AddAuthorizationBuilder()
            .SetDefaultPolicy(new AuthorizationPolicyBuilder(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    PersonalAccessTokenHandler.SchemeName,
                    UserApiBearerScheme)
                .RequireAuthenticatedUser()
                .Build())
            .AddPolicy("Node", policy => policy
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(CertificateAuthHandler.NodeScheme)
                .RequireRole("Node"))
            .AddPolicy(ServerPermissions.Settings_Read, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", ServerPermissions.Settings_Read))
            .AddPolicy(ServerPermissions.Settings_Write, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", ServerPermissions.Settings_Write))
            .AddPolicy(ServerPermissions.Users_Manage, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", ServerPermissions.Users_Manage))
            .AddPolicy(ServerPermissions.Groups_Manage, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", ServerPermissions.Groups_Manage))
            .AddPolicy(ServerPermissions.Roles_Manage, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", ServerPermissions.Roles_Manage))
            .AddPolicy(ServerPermissions.RegistrationKeys_Manage, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", ServerPermissions.RegistrationKeys_Manage))
            .AddPolicy(NodePermissions.Read, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", NodePermissions.Read))
            .AddPolicy(NodePermissions.Write, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", NodePermissions.Write))
            .AddPolicy(NodePermissions.Delete, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", NodePermissions.Delete))
            .AddPolicy(NodePermissions.AssignConfiguration, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", NodePermissions.AssignConfiguration))
            .AddPolicy(ReportPermissions.Read, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", ReportPermissions.Read))
            .AddPolicy(ReportPermissions.ReadAll, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", ReportPermissions.ReadAll))
            .AddPolicy(RetentionPermissions.Manage, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", RetentionPermissions.Manage))
            .AddPolicy(ConfigurationPermissions.AdminOverride, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", ConfigurationPermissions.AdminOverride))
            .AddPolicy(CompositeConfigurationPermissions.AdminOverride, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", CompositeConfigurationPermissions.AdminOverride))
            .AddPolicy(ParameterPermissions.AdminOverride, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", ParameterPermissions.AdminOverride))
            .AddPolicy(ScopePermissions.AdminOverride, policy => policy
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, PersonalAccessTokenHandler.SchemeName, UserApiBearerScheme)
                .RequireClaim("permission", ScopePermissions.AdminOverride));

        return services;
    }

    internal static string OidcSchemeName(string providerName) => $"oidc_{providerName}";

    internal static string JwtSchemeName(string providerName) => $"jwt_{providerName}";

    internal static async Task HandleJwtTokenValidatedAsync(
        JwtBearerTokenValidatedContext context,
        string providerName)
    {
        var db = context.HttpContext.RequestServices
            .GetRequiredService<ServerDbContext>();

        var sub = context.Principal!.FindFirstValue("sub")
            ?? context.Principal!.FindFirstValue(ClaimTypes.NameIdentifier);

        if (sub is null)
        {
            context.Fail("JWT is missing sub claim.");
            return;
        }

        var user = await db.ExternalLogins
            .Where(el => el.Provider == providerName && el.ProviderKey == sub)
            .Join(db.Users, el => el.UserId, u => u.Id, (el, u) => u)
            .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

        if (user is null)
        {
            context.Fail("No local user found for this OIDC token.");
            return;
        }

        var identity = context.Principal!.Identity as ClaimsIdentity
            ?? throw new InvalidOperationException("JWT principal has no ClaimsIdentity.");
        foreach (var existing in identity.FindAll(ClaimTypes.NameIdentifier).ToList())
            identity.RemoveClaim(existing);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
    }

    internal static async Task HandleOidcTokenValidatedAsync(
        OpenIdConnectTokenValidatedContext context,
        string providerName,
        IOidcUserProvisioningService provisioningService)
    {
        var principal = context.Principal!;
        var providerKey = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("OIDC token missing subject claim.");

        var email = principal.FindFirstValue(ClaimTypes.Email)
            ?? principal.FindFirstValue("email");
        var displayName = principal.FindFirstValue(ClaimTypes.Name)
            ?? principal.FindFirstValue("name");
        var preferredUsername = principal.FindFirstValue("preferred_username")
            ?? email;

        var user = await provisioningService.ProvisionOrGetUserAsync(
            providerName,
            providerKey,
            displayName,
            email,
            preferredUsername,
            context.HttpContext.RequestAborted);

        var identity = context.Principal!.Identity as ClaimsIdentity
            ?? throw new InvalidOperationException("OIDC principal has no ClaimsIdentity.");
        foreach (var existing in identity.FindAll(ClaimTypes.NameIdentifier).ToList())
            identity.RemoveClaim(existing);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
    }

    internal static string? TryGetJwtIssuer(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwt = handler.ReadJwtToken(token);
                return jwt.Issuer;
            }
        }
        catch
        {
            // Not a valid JWT — fall through
        }

        return null;
    }
}
