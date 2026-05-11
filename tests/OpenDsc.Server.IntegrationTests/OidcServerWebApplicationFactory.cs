// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using OpenDsc.Contracts.Users;
using OpenDsc.Server.Authentication;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.IntegrationTests;

/// <summary>
/// A <see cref="ServerWebApplicationFactory"/> variant that pre-configures one OIDC provider
/// with a mocked backchannel HTTP handler and adds a test-only authentication bypass scheme
/// so integration tests can authenticate as specific users without going through the OIDC flow.
/// </summary>
internal sealed class OidcServerWebApplicationFactory : ServerWebApplicationFactory
{
    internal const string ProviderName = "testprovider";
    internal const string ProviderDisplayName = "Test Provider";
    private const string TestAuthority = "https://test-oidc-provider";
    private const string TestAuthScheme = "TestScheme";
    private const string TestPolicyScheme = "TestPolicy";

    private static readonly SymmetricSecurityKey TestSigningKey =
        new(Encoding.UTF8.GetBytes("test-signing-key-that-is-32-bytes!"));

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"Authentication:OidcProviders:0:Name"] = ProviderName,
                [$"Authentication:OidcProviders:0:DisplayName"] = ProviderDisplayName,
                [$"Authentication:OidcProviders:0:Authority"] = TestAuthority,
                [$"Authentication:OidcProviders:0:ClientId"] = "test-client",
                [$"Authentication:OidcProviders:0:ClientSecret"] = "test-secret",
            });
        });

        builder.ConfigureServices(services =>
        {
            // AddServerAuthentication reads OidcProviders from IConfiguration before
            // ConfigureAppConfiguration additions take effect. Register the test OIDC scheme
            // explicitly here so the scheme exists when the challenge endpoint runs.
            services.AddAuthentication()
                .AddOpenIdConnect("oidc_" + ProviderName, options =>
                {
                    options.Authority = TestAuthority;
                    options.ClientId = "test-client";
                    options.ClientSecret = "test-secret";
                    options.ResponseType = "code";
                    options.BackchannelHttpHandler = new MockOidcBackchannelHandler();
                    options.RequireHttpsMetadata = false;
                });

            // Register the JWT bearer scheme for the test provider using a known symmetric
            // key, and wire the production HandleJwtTokenValidatedAsync handler so the
            // production code paths are exercised by integration tests.
            services.AddAuthentication()
                .AddJwtBearer(AuthenticationExtensions.JwtSchemeName(ProviderName), options =>
                {
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = TestSigningKey,
                        ValidIssuer = TestAuthority,
                        ValidAudience = "test-client",
                        ValidateLifetime = false,
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = ctx =>
                            AuthenticationExtensions.HandleJwtTokenValidatedAsync(ctx, ProviderName)
                    };
                });

            // Override UserApiBearerScheme so that Bearer JWTs from the test authority are
            // forwarded to the test JWT bearer scheme (mirrors the production policy scheme
            // that would be registered when OIDC providers are present in config at startup).
            services.PostConfigure<PolicySchemeOptions>(
                AuthenticationExtensions.UserApiBearerScheme, opts =>
                {
                    opts.ForwardDefaultSelector = ctx =>
                    {
                        var authHeader = ctx.Request.Headers.Authorization.ToString();

                        if (authHeader.StartsWith("Bearer pat_", StringComparison.OrdinalIgnoreCase))
                        {
                            return PersonalAccessTokenHandler.SchemeName;
                        }

                        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            var token = authHeader["Bearer ".Length..].Trim();
                            var issuer = AuthenticationExtensions.TryGetJwtIssuer(token);
                            if (issuer?.TrimEnd('/').Equals(TestAuthority.TrimEnd('/'),
                                    StringComparison.OrdinalIgnoreCase) == true)
                            {
                                return AuthenticationExtensions.JwtSchemeName(ProviderName);
                            }
                        }

                        return CookieAuthenticationDefaults.AuthenticationScheme;
                    };
                });

            // Add a header-driven test auth scheme for authenticating as any user in tests
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthScheme, null)
                .AddPolicyScheme(TestPolicyScheme, null, policy =>
                {
                    policy.ForwardDefaultSelector = ctx =>
                        ctx.Request.Headers.ContainsKey(TestAuthHandler.UserIdHeader)
                            ? TestAuthScheme
                            : CookieAuthenticationDefaults.AuthenticationScheme;
                });

            // Override the default authenticate scheme to the policy scheme so the test
            // handler is consulted before the cookie handler on each request.
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestPolicyScheme;
            });

            // Include the test scheme in the default authorization policy so authenticated
            // test users are accepted the same as cookie/PAT users.
            services.PostConfigure<AuthorizationOptions>(options =>
            {
                var existing = options.DefaultPolicy;
                options.DefaultPolicy = new AuthorizationPolicyBuilder(existing)
                    .AddAuthenticationSchemes(TestAuthScheme, TestPolicyScheme)
                    .Build();
            });
        });
    }

    /// <summary>
    /// Creates a signed JWT for integration tests using the test symmetric key.
    /// </summary>
    public string CreateTestJwt(string sub)
    {
        var credentials = new SigningCredentials(TestSigningKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: TestAuthority,
            audience: "test-client",
            claims: [new Claim("sub", sub)],
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates a signed JWT that intentionally omits the <c>sub</c> claim.
    /// </summary>
    public string CreateTestJwtWithoutSub()
    {
        var credentials = new SigningCredentials(TestSigningKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: TestAuthority,
            audience: "test-client",
            claims: [new Claim("email", "no-sub@example.com")],
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Creates an HTTP client whose every request authenticates as the given user ID via
    /// the <see cref="TestAuthHandler"/> header bypass — no cookie or OIDC flow required.
    /// </summary>
    public HttpClient CreateClientAuthenticatedAs(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        return client;
    }

    /// <summary>
    /// Seeds an OIDC-only user (null PasswordHash) and a corresponding ExternalLogin record.
    /// </summary>
    public async Task<Guid> SeedOidcUserAsync(
        string provider,
        string providerKey,
        string username,
        string email)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            IsActive = true,
            RequirePasswordChange = false,
            AccountType = AccountType.User,
            CreatedAt = DateTimeOffset.UtcNow,
            // PasswordHash intentionally null — OIDC-only user
        };

        db.Users.Add(user);
        db.ExternalLogins.Add(new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = provider,
            ProviderKey = providerKey,
            ProviderDisplayName = provider,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();
        return user.Id;
    }
}

/// <summary>
/// An <see cref="HttpMessageHandler"/> that intercepts OIDC backchannel requests and returns
/// a minimal discovery document and JWKS without making real network calls.
/// </summary>
internal sealed class MockOidcBackchannelHandler : HttpMessageHandler
{
    private const string Authority = "https://test-oidc-provider";

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (path.EndsWith("/.well-known/openid-configuration", StringComparison.OrdinalIgnoreCase))
        {
            var discoveryDoc = JsonSerializer.Serialize(new
            {
                issuer = Authority,
                authorization_endpoint = $"{Authority}/authorize",
                token_endpoint = $"{Authority}/token",
                userinfo_endpoint = $"{Authority}/userinfo",
                jwks_uri = $"{Authority}/.well-known/keys",
                response_types_supported = new[] { "code" },
                subject_types_supported = new[] { "public" },
                id_token_signing_alg_values_supported = new[] { "RS256" },
                scopes_supported = new[] { "openid", "profile", "email" },
            });

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(discoveryDoc, Encoding.UTF8, "application/json"),
            });
        }

        if (path.EndsWith("/.well-known/keys", StringComparison.OrdinalIgnoreCase))
        {
            var jwks = JsonSerializer.Serialize(new { keys = Array.Empty<object>() });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jwks, Encoding.UTF8, "application/json"),
            });
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}

/// <summary>
/// An authentication handler that reads the <c>X-Test-User-Id</c> request header and
/// creates an authenticated principal for that user — for use in integration tests only.
/// </summary>
internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string UserIdHeader = "X-Test-User-Id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var headerValue) ||
            !Guid.TryParse(headerValue, out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

