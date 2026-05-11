// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Json;

using AwesomeAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

/// <summary>
/// Tests for <c>GET /api/v1/auth/oidc/providers</c> when no OIDC providers are configured.
/// Uses the default factory so OIDC is not configured.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OidcEndpoints_NoProviders_Tests : IAsyncLifetime
{
    private readonly ServerWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        await Task.CompletedTask;
        _client = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetProviders_NoOidcConfig_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/api/v1/auth/oidc/providers", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var providers = await response.Content.ReadFromJsonAsync<OidcProviderDto[]>(TestContext.Current.CancellationToken);
        providers.Should().NotBeNull();
        providers.Should().BeEmpty();
    }
}

/// <summary>
/// Tests for OIDC endpoints with one provider configured via <see cref="OidcServerWebApplicationFactory"/>.
/// Also tests authentication endpoint guard behaviour for OIDC users (null PasswordHash).
/// </summary>
[Trait("Category", "Integration")]
public sealed class OidcEndpointsTests : IAsyncLifetime
{
    private readonly OidcServerWebApplicationFactory _factory = new();

    private Guid _oidcUserId;
    private HttpClient _unauthClient = null!;
    private HttpClient _oidcUserClient = null!;
    private HttpClient _localUserClient = null!;

    public async ValueTask InitializeAsync()
    {
        _oidcUserId = await _factory.SeedOidcUserAsync(
            provider: OidcServerWebApplicationFactory.ProviderName,
            providerKey: "test-sub-integration",
            username: "oidc-test-user",
            email: "oidc-test-user@example.com");

        _unauthClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        _oidcUserClient = _factory.CreateClientAuthenticatedAs(_oidcUserId);
        _localUserClient = await _factory.CreateAuthenticatedClientAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _unauthClient.Dispose();
        _oidcUserClient.Dispose();
        _localUserClient.Dispose();
        await _factory.DisposeAsync();
    }

    // ─── /api/v1/auth/oidc/providers ─────────────────────────────────────────

    [Fact]
    public async Task GetProviders_WithOidcConfig_ReturnsList()
    {
        var response = await _unauthClient.GetAsync("/api/v1/auth/oidc/providers", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var providers = await response.Content.ReadFromJsonAsync<OidcProviderDto[]>(TestContext.Current.CancellationToken);
        providers.Should().NotBeNull();
        providers.Should().HaveCount(1);
        providers![0].Name.Should().Be(OidcServerWebApplicationFactory.ProviderName);
        providers[0].DisplayName.Should().Be(OidcServerWebApplicationFactory.ProviderDisplayName);
    }

    // ─── /api/v1/auth/oidc/{provider}/challenge ──────────────────────────────

    [Fact]
    public async Task Challenge_UnknownProvider_ReturnsNotFound()
    {
        var response = await _unauthClient.GetAsync(
            "/api/v1/auth/oidc/nonexistent/challenge",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Challenge_KnownProvider_RedirectsToOidcAuthorizationEndpoint()
    {
        // AllowAutoRedirect=false so we can inspect the 302 directly
        var response = await _unauthClient.GetAsync(
            $"/api/v1/auth/oidc/{OidcServerWebApplicationFactory.ProviderName}/challenge",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("test-oidc-provider");
        response.Headers.Location!.ToString().Should().Contain("/authorize");
    }

    [Fact]
    public async Task Challenge_KnownProvider_WithReturnUrl_IncludesStateInRedirect()
    {
        var response = await _unauthClient.GetAsync(
            $"/api/v1/auth/oidc/{OidcServerWebApplicationFactory.ProviderName}/challenge?returnUrl=%2Fdashboard",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location.Should().NotBeNull();
        // The OIDC middleware encodes the returnUrl in the state parameter
        response.Headers.Location!.ToString().Should().Contain("state=");
    }

    // ─── Login guard: OIDC users cannot log in via password ──────────────────

    [Fact]
    public async Task Login_OidcUser_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { username = "oidc-test-user", password = "anything" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── Change password guard: OIDC users cannot change password ────────────

    [Fact]
    public async Task ChangePassword_OidcUser_ReturnsBadRequest()
    {
        var response = await _oidcUserClient.PostAsJsonAsync(
            "/api/v1/auth/change-password",
            new { currentPassword = "anything", newPassword = "NewPassword123!" },
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.Should().Contain("Password changes are not available");
    }

    // ─── GET /api/v1/auth/me ─────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentUser_OidcUser_IncludesAuthProvider()
    {
        var response = await _oidcUserClient.GetAsync(
            "/api/v1/auth/me",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<CurrentUserDto>(TestContext.Current.CancellationToken);
        user.Should().NotBeNull();
        user!.AuthProvider.Should().Be(OidcServerWebApplicationFactory.ProviderName);
    }

    [Fact]
    public async Task GetCurrentUser_LocalUser_AuthProviderIsNull()
    {
        var response = await _localUserClient.GetAsync(
            "/api/v1/auth/me",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<CurrentUserDto>(TestContext.Current.CancellationToken);
        user.Should().NotBeNull();
        user!.AuthProvider.Should().BeNull();
    }
}

/// <summary>
/// Tests for JWT bearer authentication via the <c>UserApiBearer</c> policy scheme.
/// Covers <c>HandleJwtTokenValidatedAsync</c> and the JWT routing in <c>UserApiBearerScheme</c>.
/// </summary>
[Trait("Category", "Integration")]
public sealed class JwtBearerEndpointsTests : IAsyncLifetime
{
    private readonly OidcServerWebApplicationFactory _factory = new();
    private const string JwtTestSub = "jwt-integration-sub-001";
    private Guid _oidcUserId;

    public async ValueTask InitializeAsync()
    {
        _oidcUserId = await _factory.SeedOidcUserAsync(
            provider: OidcServerWebApplicationFactory.ProviderName,
            providerKey: JwtTestSub,
            username: "jwt-bearer-test-user",
            email: "jwt-bearer@example.com");
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    // ─── JWT bearer: UserApiBearerScheme routing + HandleJwtTokenValidatedAsync ──

    [Fact]
    public async Task JwtBearer_ValidToken_UserFound_Returns200()
    {
        var jwt = _factory.CreateTestJwt(JwtTestSub);

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.GetAsync(
            "/api/v1/configurations",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task JwtBearer_ValidToken_UserNotFound_Returns401()
    {
        var jwt = _factory.CreateTestJwt("unknown-sub-that-has-no-external-login");

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.GetAsync(
            "/api/v1/configurations",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task JwtBearer_TokenMissingSub_Returns401()
    {
        var jwt = _factory.CreateTestJwtWithoutSub();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwt);

        var response = await client.GetAsync(
            "/api/v1/configurations",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

/// <summary>
/// Tests for <see cref="PasswordChangeEnforcementMiddleware"/> OIDC skip path.
/// An OIDC user with <c>RequirePasswordChange=true</c> and <c>PasswordHash=null</c>
/// must not be blocked by the middleware.
/// </summary>
[Trait("Category", "Integration")]
public sealed class OidcMiddlewareTests : IAsyncLifetime
{
    private readonly OidcServerWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OpenDsc.Server.Data.ServerDbContext>();

        var userId = Guid.NewGuid();
        db.Users.Add(new OpenDsc.Server.Entities.User
        {
            Id = userId,
            Username = "oidc-pw-change-user",
            Email = "oidc-pw-change@example.com",
            IsActive = true,
            RequirePasswordChange = true,
            PasswordHash = null,
            AccountType = OpenDsc.Contracts.Users.AccountType.User,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        _client = _factory.CreateClientAuthenticatedAs(userId);
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task PasswordChangeEnforcement_OidcUser_NullPasswordHash_AllowsAccess()
    {
        // Hitting /api/v1/configurations exercises the middleware body for an authenticated
        // user on a non-exempt path. Because PasswordHash is null the middleware must pass
        // through even when RequirePasswordChange is true.
        var response = await _client.GetAsync(
            "/api/v1/configurations",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}

// ─── Local DTO types for response deserialization ────────────────────────────

file record OidcProviderDto(string Name, string DisplayName);

file record CurrentUserDto(
    Guid UserId,
    string Username,
    string Email,
    string AccountType,
    List<string> Roles,
    string? AuthProvider);
