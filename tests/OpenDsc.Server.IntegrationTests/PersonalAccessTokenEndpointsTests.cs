// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Server.Endpoints;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class PersonalAccessTokenEndpointsTests : IClassFixture<ServerWebApplicationFactory>
{
    private readonly ServerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PersonalAccessTokenEndpointsTests(ServerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetPersonalAccessTokens_ReturnsTokenList()
    {
        var response = await _client.GetAsync("/api/v1/auth/tokens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await response.Content.ReadFromJsonAsync<List<TokenMetadata>>();
        tokens.Should().NotBeNull();
        // Should contain at least the token used for authentication
        tokens!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreatePersonalAccessToken_WithValidData_CreatesToken()
    {
        var createRequest = new CreateTokenRequest
        {
            Name = "TestToken",
            Scopes = ["read", "write"],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/tokens", createRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var token = await response.Content.ReadFromJsonAsync<CreateTokenResponse>();
        token.Should().NotBeNull();
        token!.Name.Should().Be("TestToken");
        token.Token.Should().NotBeNullOrEmpty(); // The actual token value
        token.TokenId.Should().NotBeEmpty();
        token.TokenPrefix.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetPersonalAccessToken_WithValidId_ReturnsTokenDetails()
    {
        // Create a test token first
        var createRequest = new CreateTokenRequest
        {
            Name = "GetToken",
            Scopes = ["read"],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/auth/tokens", createRequest);
        var createdToken = await createResponse.Content.ReadFromJsonAsync<CreateTokenResponse>();

        var response = await _client.GetAsync("/api/v1/auth/tokens");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await response.Content.ReadFromJsonAsync<List<TokenMetadata>>();
        var token = tokens!.First(t => t.Name == "GetToken");
        token.Should().NotBeNull();
        token!.Name.Should().Be("GetToken");
        token.Id.Should().Be(createdToken!.TokenId);
    }

    [Fact]
    public async Task DeletePersonalAccessToken_WithValidId_DeletesToken()
    {
        // Create a test token first
        var createRequest = new CreateTokenRequest
        {
            Name = "DeleteToken",
            Scopes = ["read"],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/auth/tokens", createRequest);
        var createdToken = await createResponse.Content.ReadFromJsonAsync<CreateTokenResponse>();

        // Delete the token
        var deleteResponse = await _client.DeleteAsync($"/api/v1/auth/tokens/{createdToken!.TokenId}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify token is revoked (should still exist but be marked as revoked)
        var getResponse = await _client.GetAsync("/api/v1/auth/tokens");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await getResponse.Content.ReadFromJsonAsync<List<TokenMetadata>>();
        var deletedToken = tokens!.FirstOrDefault(t => t.Id == createdToken.TokenId);
        deletedToken.Should().NotBeNull();
        deletedToken!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokePersonalAccessToken_WithValidId_RevokesToken()
    {
        // Create a test token first
        var createRequest = new CreateTokenRequest
        {
            Name = "RevokeToken",
            Scopes = ["read"],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/auth/tokens", createRequest);
        var createdToken = await createResponse.Content.ReadFromJsonAsync<CreateTokenResponse>();

        // Revoke the token
        var revokeResponse = await _client.DeleteAsync($"/api/v1/auth/tokens/{createdToken!.TokenId}");

        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify token is revoked (should still exist but be marked as revoked)
        var getResponse = await _client.GetAsync("/api/v1/auth/tokens");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await getResponse.Content.ReadFromJsonAsync<List<TokenMetadata>>();
        var revokedToken = tokens!.First(t => t.Id == createdToken.TokenId);
        revokedToken.IsRevoked.Should().BeTrue();
    }
}
