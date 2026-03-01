// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class AuthenticationEndpointsTests : IAsyncLifetime, IDisposable
{
    private readonly ServerWebApplicationFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _client = await _factory.CreateAuthenticatedClientAsync();
    }

    public Task DisposeAsync()
    {
        _client?.Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsSuccess()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var loginRequest = new { username = "admin", password = "admin" };
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        result.Should().NotBeNull();
        result!.Username.Should().Be("admin");
        result.RequirePasswordChange.Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var loginRequest = new { username = "admin", password = "wrongpassword" };
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateToken_WithValidSession_ReturnsToken()
    {
        var tokenRequest = new { name = "Test Token", expiresAt = (DateTimeOffset?)null };
        var response = await _client.PostAsJsonAsync("/api/v1/auth/tokens", tokenRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TokenResponse>();
        result.Should().NotBeNull();
        result!.Token.Should().StartWith("pat_");
        result.Token.Length.Should().Be(44);
        result.Name.Should().Be("Test Token");
    }

    [Fact]
    public async Task ListTokens_ReturnsUserTokens()
    {
        // Create a test token
        await _client.PostAsJsonAsync("/api/v1/auth/tokens", new { name = "List Test Token", expiresAt = (DateTimeOffset?)null });

        var response = await _client.GetAsync("/api/v1/auth/tokens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await response.Content.ReadFromJsonAsync<List<TokenSummary>>();
        tokens.Should().NotBeNull();
        tokens!.Should().Contain(t => t.Name == "List Test Token");
    }

    [Fact]
    public async Task RevokeToken_DeletesToken()
    {
        // Create a token
        var createResponse = await _client.PostAsJsonAsync("/api/v1/auth/tokens",
            new { name = "Revoke Test Token", expiresAt = (DateTimeOffset?)null });

        // Get tokens to find the ID
        var listResponse = await _client.GetAsync("/api/v1/auth/tokens");
        var tokens = await listResponse.Content.ReadFromJsonAsync<List<TokenSummary>>();
        var tokenToRevoke = tokens!.First(t => t.Name == "Revoke Test Token");

        // Revoke the token
        var revokeResponse = await _client.DeleteAsync($"/api/v1/auth/tokens/{tokenToRevoke.Id}");

        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify token is revoked (should still exist but be marked as revoked)
        var verifyResponse = await _client.GetAsync("/api/v1/auth/tokens");
        var remainingTokens = await verifyResponse.Content.ReadFromJsonAsync<List<TokenSummary>>();
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
        await cookieClient.PostAsJsonAsync("/api/v1/auth/login", new { username = "admin", password = "admin" });

        // Change password
        var changeRequest = new { currentPassword = "admin", newPassword = "NewPassword123!" };
        var response = await cookieClient.PostAsJsonAsync("/api/v1/auth/change-password", changeRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify new password works
        using var newClient = _factory.CreateClient();
        var loginResponse = await newClient.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "admin", password = "NewPassword123!" });
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
        await cookieClient.PostAsJsonAsync("/api/v1/auth/login", new { username = "admin", password = "admin" });

        // Logout
        var logoutResponse = await cookieClient.PostAsync("/api/v1/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify session is cleared
        var testResponse = await cookieClient.GetAsync("/api/v1/auth/tokens");
        testResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private record LoginResponse(string Username, string Email, bool RequirePasswordChange);
    private record TokenResponse(string Token, string Name, DateTimeOffset? ExpiresAt);
    private record TokenSummary(Guid Id, string Name, DateTimeOffset CreatedAt, DateTimeOffset? ExpiresAt, DateTimeOffset? LastUsedAt, bool IsRevoked);
}
