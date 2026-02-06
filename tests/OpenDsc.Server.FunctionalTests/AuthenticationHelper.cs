// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Mvc.Testing;

using OpenDsc.Server.Endpoints;

namespace OpenDsc.Server.FunctionalTests;

public static class AuthenticationHelper
{
    public static async Task<string> GetAdminTokenAsync(WebApplicationFactory<Program> factory)
    {
        // Create a client that handles cookies to maintain login session
        var clientOptions = new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            AllowAutoRedirect = true
        };
        var client = factory.CreateClient(clientOptions);

        // Login with default admin credentials
        var loginRequest = new LoginRequest
        {
            Username = "admin",
            Password = "admin"
        };

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        // Check if login failed
        if (!loginResponse.IsSuccessStatusCode)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Login failed with status {loginResponse.StatusCode}: {errorContent}");
        }

        // Create a personal access token (uses cookie auth from login)
        var tokenRequest = new CreateTokenRequest
        {
            Name = $"test-token-{Guid.NewGuid()}",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            Scopes = ["*"] // Full access
        };

        var tokenResponse = await client.PostAsJsonAsync("/api/v1/auth/tokens", tokenRequest);

        // Check if token creation failed
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorContent = await tokenResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Token creation failed with status {tokenResponse.StatusCode}: {errorContent}");
        }

        var tokenResult = await tokenResponse.Content.ReadFromJsonAsync<CreateTokenResponse>();
        return tokenResult!.Token;
    }

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(WebApplicationFactory<Program> factory)
    {
        var token = await GetAdminTokenAsync(factory);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
