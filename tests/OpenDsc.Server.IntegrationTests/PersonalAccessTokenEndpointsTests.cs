// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Headers;

using AwesomeAssertions;

using OpenDsc.Server.Authorization;
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
        var response = await _client.GetAsync("/api/v1/auth/tokens", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await response.Content.ReadFromJsonAsync<List<TokenMetadata>>(TestContext.Current.CancellationToken);
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
            Scopes = ["nodes.read", "nodes.write"],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        var response = await _client.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var token = await response.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);
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
            Scopes = ["nodes.read"],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);
        var createdToken = await createResponse.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);

        var response = await _client.GetAsync("/api/v1/auth/tokens", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await response.Content.ReadFromJsonAsync<List<TokenMetadata>>(TestContext.Current.CancellationToken);
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
            Scopes = ["nodes.read"],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);
        var createdToken = await createResponse.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);

        // Delete the token
        var deleteResponse = await _client.DeleteAsync($"/api/v1/auth/tokens/{createdToken!.TokenId}", TestContext.Current.CancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify token is revoked (should still exist but be marked as revoked)
        var getResponse = await _client.GetAsync("/api/v1/auth/tokens", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await getResponse.Content.ReadFromJsonAsync<List<TokenMetadata>>(TestContext.Current.CancellationToken);
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
            Scopes = ["nodes.read"],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);
        var createdToken = await createResponse.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);

        // Revoke the token
        var revokeResponse = await _client.DeleteAsync($"/api/v1/auth/tokens/{createdToken!.TokenId}", TestContext.Current.CancellationToken);

        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify token is revoked (should still exist but be marked as revoked)
        var getResponse = await _client.GetAsync("/api/v1/auth/tokens", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var tokens = await getResponse.Content.ReadFromJsonAsync<List<TokenMetadata>>(TestContext.Current.CancellationToken);
        var revokedToken = tokens!.First(t => t.Id == createdToken.TokenId);
        revokedToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task PatToken_WithReadScope_CannotPerformWriteOperation()
    {
        var roleClient = await _factory.CreateUserWithPermissionsAsync(
            username: "pat-scope-user",
            permissions: [Permissions.Nodes_Read, Permissions.Nodes_Delete]);

        var createRequest = new CreateTokenRequest
        {
            Name = "ReadOnlyNodesToken",
            Scopes = [Permissions.Nodes_Read],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        var createResponse = await roleClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdToken = await createResponse.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);
        createdToken.Should().NotBeNull();

        using var patClient = _factory.CreateClient();
        patClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", createdToken!.Token);

        var deleteResponse = await patClient.DeleteAsync($"/api/v1/nodes/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
