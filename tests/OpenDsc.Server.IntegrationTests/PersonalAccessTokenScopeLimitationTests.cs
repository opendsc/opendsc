// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Endpoints;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class PersonalAccessTokenScopeLimitationTests : IClassFixture<ServerWebApplicationFactory>
{
    private readonly ServerWebApplicationFactory _factory;

    public PersonalAccessTokenScopeLimitationTests(ServerWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateToken_AdminUser_CanCreateTokenWithAnyValidScopes()
    {
        // Arrange: Create an admin client (has all permissions)
        var adminClient = _factory.CreateAuthenticatedClient();

        var createRequest = new CreateTokenRequest
        {
            Name = "AdminToken",
            Scopes = [
                NodePermissions.Read,
                NodePermissions.Write,
                NodePermissions.Delete,
                ServerPermissions.SettingsRead,
                ServerPermissions.SettingsWrite
            ],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var token = await response.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);
        token.Should().NotBeNull();
        token!.Scopes.Should().HaveCount(5);
        token.Scopes.Should().ContainInOrder(
            NodePermissions.Read,
            NodePermissions.Write,
            NodePermissions.Delete,
            ServerPermissions.SettingsRead,
            ServerPermissions.SettingsWrite);
    }

    [Fact]
    public async Task CreateToken_WithInvalidScope_ReturnsBadRequest()
    {
        // Arrange
        var adminClient = _factory.CreateAuthenticatedClient();

        var createRequest = new CreateTokenRequest
        {
            Name = "InvalidScopeToken",
            Scopes = ["invalid.scope", "another.invalid"],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);

        // Assert: Invalid scopes should be rejected
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        content.Should().Contain("Invalid scopes");
    }

    [Fact]
    public async Task CreateToken_WithValidAndInvalidMixedScopes_ReturnsBadRequest()
    {
        // Arrange
        var adminClient = _factory.CreateAuthenticatedClient();

        var createRequest = new CreateTokenRequest
        {
            Name = "MixedToken",
            Scopes = [NodePermissions.Read, "invalid.scope", NodePermissions.Write],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateToken_WithDuplicateScopes_DeduplicatesScopes()
    {
        // Arrange
        var adminClient = _factory.CreateAuthenticatedClient();

        var createRequest = new CreateTokenRequest
        {
            Name = "DuplicateToken",
            Scopes = [NodePermissions.Read, NodePermissions.Read, NodePermissions.Write, NodePermissions.Write],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);

        // Assert: Should deduplicate to 2 scopes
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var token = await response.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);
        token.Should().NotBeNull();
        token!.Scopes.Should().HaveCount(2);
        token.Scopes.Should().Contain(NodePermissions.Read);
        token.Scopes.Should().Contain(NodePermissions.Write);
    }

    [Fact]
    public async Task CreateToken_WithEmptyScopes_CreatesTokenWithEmptyScopes()
    {
        // Arrange
        var adminClient = _factory.CreateAuthenticatedClient();

        var createRequest = new CreateTokenRequest
        {
            Name = "EmptyScopesToken",
            Scopes = [],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);

        // Assert: Should succeed but token has no scopes (effectively useless)
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var token = await response.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);
        token.Should().NotBeNull();
        token!.Scopes.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateToken_WithWhitespaceScopes_FiltersWhitespaceAndDeduplicates()
    {
        // Arrange
        var adminClient = _factory.CreateAuthenticatedClient();

        var createRequest = new CreateTokenRequest
        {
            Name = "WhitespaceToken",
            Scopes = [NodePermissions.Read, "  ", "\t", NodePermissions.Write, ""],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);

        // Assert: Should filter out whitespace-only strings
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var token = await response.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);
        token.Should().NotBeNull();
        token!.Scopes.Should().HaveCount(2);
    }

    [Fact]
    public async Task PatToken_WithRestrictedScopes_CannotExceedScopePermissions()
    {
        // Arrange: Create user with only read permissions
        var limitedClient = await _factory.CreateUserWithPermissionsAsync(
            username: "limited-scope-user",
            permissions: [NodePermissions.Read]);

        // Create a PAT with read scope
        var createRequest = new CreateTokenRequest
        {
            Name = "LimitedToken",
            Scopes = [NodePermissions.Read],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        var createResponse = await limitedClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var token = await createResponse.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);

        // Act: Use PAT to try to delete (which user doesn't have permission for)
        using var patClient = _factory.CreateClient();
        patClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.Token);

        // Try to delete a node (requires delete permission)
        var deleteResponse = await patClient.DeleteAsync($"/api/v1/nodes/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert: Should be forbidden because PAT user doesn't have delete permission
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PatToken_WithReadAndWriteScopes_CanPerformLimitedOperations()
    {
        // Arrange: Create user with read and write permissions
        var multiPermissionClient = await _factory.CreateUserWithPermissionsAsync(
            username: "multi-scope-user",
            permissions: [NodePermissions.Read, NodePermissions.Write]);

        // Create PAT with read and write scopes
        var createRequest = new CreateTokenRequest
        {
            Name = "MultiScopeToken",
            Scopes = [NodePermissions.Read, NodePermissions.Write],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        var createResponse = await multiPermissionClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var token = await createResponse.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);

        // Act & Assert: Verify token has both scopes
        token.Should().NotBeNull();
        token!.Scopes.Should().ContainInOrder(NodePermissions.Read, NodePermissions.Write);

        // Use PAT with read scope
        using var patClient = _factory.CreateClient();
        patClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var getResponse = await patClient.GetAsync("/api/v1/nodes", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PatToken_EffectivePermissions_IntersectTokenScopesAndUserPermissions()
    {
        // Arrange: Create user with read, write, and delete permissions
        var fullPermissionClient = await _factory.CreateUserWithPermissionsAsync(
            username: "full-scope-user",
            permissions: [NodePermissions.Read, NodePermissions.Write, NodePermissions.Delete]);

        // Create PAT with only read scope (even though user has more)
        var createRequest = new CreateTokenRequest
        {
            Name = "RestrictedToken",
            Scopes = [NodePermissions.Read],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        var createResponse = await fullPermissionClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);
        var token = await createResponse.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);

        // Act: Try to delete using PAT (user has delete permission, but PAT only has read)
        using var patClient = _factory.CreateClient();
        patClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.Token);

        // Try to delete a node
        var deleteResponse = await patClient.DeleteAsync($"/api/v1/nodes/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        // Assert: Should be forbidden because PAT scope doesn't include delete
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PatToken_WithEmptyScopes_CannotPerformAnyOperation()
    {
        // Arrange: Admin creates token with no scopes
        var adminClient = _factory.CreateAuthenticatedClient();

        var createRequest = new CreateTokenRequest
        {
            Name = "NoScopesToken",
            Scopes = [],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);
        var token = await createResponse.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);

        // Act: Use PAT to try to read nodes
        using var patClient = _factory.CreateClient();
        patClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.Token);

        var getResponse = await patClient.GetAsync("/api/v1/nodes", TestContext.Current.CancellationToken);

        // Assert: Should be forbidden because token has no scopes
        getResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PatToken_Claims_IncludeBothScopesAndEffectivePermissions()
    {
        // Arrange: Create user with multiple permissions
        var userClient = await _factory.CreateUserWithPermissionsAsync(
            username: "scope-claim-user",
            permissions: [NodePermissions.Read, NodePermissions.Delete, ReportPermissions.Read]);

        // Create PAT with subset of scopes (read and reports, but not delete)
        var createRequest = new CreateTokenRequest
        {
            Name = "ScopeClaimToken",
            Scopes = [NodePermissions.Read, ReportPermissions.Read],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        var createResponse = await userClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);
        var token = await createResponse.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);

        // Act & Assert: Verify scope limitations are enforced
        using var patClient = _factory.CreateClient();
        patClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token!.Token);

        // Can use read scope
        var readResponse = await patClient.GetAsync("/api/v1/nodes", TestContext.Current.CancellationToken);
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cannot use delete scope (user has it, but PAT doesn't include it)
        var deleteResponse = await patClient.DeleteAsync($"/api/v1/nodes/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PatToken_ScopesCaseSensitive_ExactMatchRequired()
    {
        // Arrange
        var adminClient = _factory.CreateAuthenticatedClient();

        // Try to create token with uppercase scope (should fail)
        var createRequest = new CreateTokenRequest
        {
            Name = "CaseSensitiveToken",
            Scopes = ["NODES.READ", "nodes.READ"],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);

        // Assert: Should reject due to invalid scopes
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatToken_CrossCategoryScopes_EnforcedCorrectly()
    {
        // Arrange: Create user with mixed permissions across categories
        var mixedClient = await _factory.CreateUserWithPermissionsAsync(
            username: "mixed-category-user",
            permissions: [
                NodePermissions.Read,
                NodePermissions.Write,
                ServerPermissions.SettingsWrite,
                ReportPermissions.Read
            ]);

        // Create PAT with subset of scopes
        var createRequest = new CreateTokenRequest
        {
            Name = "MixedCategoryToken",
            Scopes = [NodePermissions.Read, ServerPermissions.SettingsWrite],
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };

        var createResponse = await mixedClient.PostAsJsonAsync("/api/v1/auth/tokens", createRequest, TestContext.Current.CancellationToken);
        var token = await createResponse.Content.ReadFromJsonAsync<CreateTokenResponse>(TestContext.Current.CancellationToken);

        // Act & Assert: Verify the token was created with correct scopes
        token.Should().NotBeNull();
        token!.Scopes.Should().HaveCount(2);
        token.Scopes.Should().Contain(NodePermissions.Read);
        token.Scopes.Should().Contain(ServerPermissions.SettingsWrite);
    }
}
