// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using Microsoft.AspNetCore.Mvc;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class AuthenticationEndpointsTests : IAsyncLifetime
{
    private readonly ServerWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var loginRequest = new { username = "admin", password = "admin" };
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Username.Should().Be("admin");
        result.RequirePasswordChange.Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var loginRequest = new { username = "admin", password = "wrongpassword" };
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateToken_WithValidSession_ReturnsToken()
    {
        var tokenRequest = new { name = "Test Token", expiresAt = (DateTimeOffset?)null };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/tokens", tokenRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TokenResponse>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Token.Should().StartWith("pat_");
        result.Token.Length.Should().Be(44);
        result.Name.Should().Be("Test Token");
    }

    [Fact]
    public async Task CreateToken_WithInvalidScopes_ReturnsValidationProblem()
    {
        var tokenRequest = new { name = "Bad Scopes Token", scopes = new[] { "nodes.read", "fake.scope", "another.invalid" } };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/tokens", tokenRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.Should().NotBeNull();
        problem!.Errors.Should().ContainKey("scopes");
        problem.Errors["scopes"].Should().ContainSingle().Which.Should().Contain("fake.scope").And.Contain("another.invalid");
    }

    [Fact]
    public async Task CreateToken_WithValidScopes_ReturnsToken()
    {
        var tokenRequest = new { name = "Valid Scopes Token", scopes = new[] { "nodes.read", "nodes.write" } };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/tokens", tokenRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TokenResponse>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Name.Should().Be("Valid Scopes Token");
    }

    [Fact]
    public async Task CreateToken_WithDuplicateScopes_ReturnsDeduplicated()
    {
        var tokenRequest = new { name = "Dedup Token", scopes = new[] { "nodes.read", "nodes.read", "nodes.write" } };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/tokens", tokenRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TokenResponse>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Scopes.Should().BeEquivalentTo(["nodes.read", "nodes.write"]);
    }

    [Fact]
    public async Task ListTokens_ReturnsUserTokens()
    {
        // Create a test token
        await _client.PostAsJsonAsync("/api/v1/auth/tokens", new { name = "List Test Token", expiresAt = (DateTimeOffset?)null }, TestContext.Current.CancellationToken);

        var response = await _client.GetAsync("/api/v1/auth/tokens", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await response.Content.ReadFromJsonAsync<List<TokenSummary>>(TestContext.Current.CancellationToken);
        tokens.Should().NotBeNull();
        tokens!.Should().Contain(t => t.Name == "List Test Token");
    }

    [Fact]
    public async Task RevokeToken_DeletesToken()
    {
        // Create a token
        var createResponse = await _client.PostAsJsonAsync("/api/v1/auth/tokens",
            new { name = "Revoke Test Token", expiresAt = (DateTimeOffset?)null }, TestContext.Current.CancellationToken);

        // Get tokens to find the ID
        var listResponse = await _client.GetAsync("/api/v1/auth/tokens", TestContext.Current.CancellationToken);
        var tokens = await listResponse.Content.ReadFromJsonAsync<List<TokenSummary>>(TestContext.Current.CancellationToken);
        var tokenToRevoke = tokens!.First(t => t.Name == "Revoke Test Token");

        // Revoke the token
        var revokeResponse = await _client.DeleteAsync($"/api/v1/auth/tokens/{tokenToRevoke.Id}", TestContext.Current.CancellationToken);

        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify token is revoked (should still exist but be marked as revoked)
        var verifyResponse = await _client.GetAsync("/api/v1/auth/tokens", TestContext.Current.CancellationToken);
        var remainingTokens = await verifyResponse.Content.ReadFromJsonAsync<List<TokenSummary>>(TestContext.Current.CancellationToken);
        var revokedToken = remainingTokens!.FirstOrDefault(t => t.Id == tokenToRevoke.Id);
        revokedToken.Should().NotBeNull();
        revokedToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_WithValidCredentials_UpdatesPassword()
    {
        using var cookieClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        // Login
        await cookieClient.PostAsJsonAsync("/api/v1/auth/login", new { username = "admin", password = "admin" }, TestContext.Current.CancellationToken);

        // Change password
        var changeRequest = new { currentPassword = "admin", newPassword = "NewPassword123!" };
        var response = await cookieClient.PostAsJsonAsync("/api/v1/auth/change-password", changeRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify new password works
        using var newClient = _factory.CreateClient();
        var loginResponse = await newClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "admin", password = "NewPassword123!" }, TestContext.Current.CancellationToken);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_ClearsSession()
    {
        using var cookieClient = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        // Login
        await cookieClient.PostAsJsonAsync("/api/v1/auth/login", new { username = "admin", password = "admin" }, TestContext.Current.CancellationToken);

        // Logout
        var logoutResponse = await cookieClient.PostAsync("/api/v1/auth/logout", null, TestContext.Current.CancellationToken);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify session is cleared
        var testResponse = await cookieClient.GetAsync("/api/v1/auth/tokens", TestContext.Current.CancellationToken);
        testResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record LoginResponse(string Username, string Email, bool RequirePasswordChange);
    private record TokenResponse(string Token, string Name, string[] Scopes, DateTimeOffset? ExpiresAt);
    private record TokenSummary(Guid Id, string Name, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt, DateTimeOffset? LastUsedAt, bool IsRevoked);
}
