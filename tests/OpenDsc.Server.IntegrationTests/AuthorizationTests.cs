// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

/// <summary>
/// Tests that global permission policies return 403 Forbidden for authenticated users
/// who lack the required permissions.
/// </summary>
[Trait("Category", "Integration")]
public class AuthorizationTests : IAsyncLifetime
{
    private readonly ServerWebApplicationFactory _factory = new();
    private HttpClient _noPermissionsClient = null!;

    public async ValueTask InitializeAsync()
    {
        _noPermissionsClient = await _factory.CreateUnprivilegedUserAsync("authz-nopriv-user");
    }

    public async ValueTask DisposeAsync()
    {
        _noPermissionsClient?.Dispose();
        await _factory.DisposeAsync();
    }

    // ---- Node endpoints ----

    [Fact]
    public async Task GetNodes_WithoutNodesRead_ReturnsForbidden()
    {
        var response = await _noPermissionsClient.GetAsync("/api/v1/nodes", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteNode_WithoutNodesDelete_ReturnsForbidden()
    {
        var nodeId = Guid.NewGuid();
        var response = await _noPermissionsClient.DeleteAsync($"/api/v1/nodes/{nodeId}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignConfigurationToNode_WithoutAssignPermission_ReturnsForbidden()
    {
        var nodeId = Guid.NewGuid();
        var response = await _noPermissionsClient.PutAsync(
            $"/api/v1/nodes/{nodeId}/configuration",
            JsonContent.Create(new { configurationName = "test", version = "1.0.0" }), TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetNodeParameterProvenance_WithoutNodesRead_ReturnsForbidden()
    {
        var nodeId = Guid.NewGuid();
        var response = await _noPermissionsClient.GetAsync($"/api/v1/nodes/{nodeId}/parameters/provenance", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetNodeParameterResolution_WithoutNodesRead_ReturnsForbidden()
    {
        var nodeId = Guid.NewGuid();
        var response = await _noPermissionsClient.GetAsync($"/api/v1/nodes/{nodeId}/parameters/resolution", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- User endpoints ----

    [Fact]
    public async Task GetUsers_WithoutUsersManage_ReturnsForbidden()
    {
        var response = await _noPermissionsClient.GetAsync("/api/v1/users", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_WithoutUsersManage_ReturnsForbidden()
    {
        var response = await _noPermissionsClient.PostAsJsonAsync("/api/v1/users",
            new { username = "x", email = "x@x.com", password = "Passw0rd!" }, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Group endpoints ----

    [Fact]
    public async Task GetGroups_WithoutGroupsManage_ReturnsForbidden()
    {
        var response = await _noPermissionsClient.GetAsync("/api/v1/groups", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Role endpoints ----

    [Fact]
    public async Task GetRoles_WithoutRolesManage_ReturnsForbidden()
    {
        var response = await _noPermissionsClient.GetAsync("/api/v1/roles", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Registration keys ----

    [Fact]
    public async Task GetRegistrationKeys_WithoutRegistrationKeysManage_ReturnsForbidden()
    {
        var response = await _noPermissionsClient.GetAsync("/api/v1/admin/registration-keys", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Reports ----

    [Fact]
    public async Task GetReports_WithoutReportsRead_ReturnsForbidden()
    {
        var response = await _noPermissionsClient.GetAsync("/api/v1/reports", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Settings ----

    [Fact]
    public async Task UpdateSettings_WithoutServerSettingsWrite_ReturnsForbidden()
    {
        var response = await _noPermissionsClient.PutAsJsonAsync(
            "/api/v1/settings",
            new { maxNodeRegistrations = 100 }, TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Scope types ----

    [Fact]
    public async Task GetScopeTypes_WithoutScopesAdminOverride_ReturnsForbidden()
    {
        var response = await _noPermissionsClient.GetAsync("/api/v1/scope-types", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Scope values ----

    [Fact]
    public async Task GetScopeValues_WithoutScopesAdminOverride_ReturnsForbidden()
    {
        var response = await _noPermissionsClient.GetAsync("/api/v1/scope-types/00000000-0000-0000-0000-000000000001/values", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Node tags ----

    [Fact]
    public async Task GetNodeTags_WithoutScopesAdminOverride_ReturnsForbidden()
    {
        var response = await _noPermissionsClient.GetAsync("/api/v1/nodes/00000000-0000-0000-0000-000000000001/tags", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Retention ----

    [Fact]
    public async Task GetRetentionPolicy_WithoutRetentionManage_ReturnsForbidden()
    {
        var response = await _noPermissionsClient.GetAsync("/api/v1/retention/runs", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---- Configurations (require auth but ACL enforced, list returns empty not 403) ----

    [Fact]
    public async Task GetConfigurations_WithoutAnyPermission_ReturnsEmptyList()
    {
        // Configurations use ACLs - unauthenticated returns 401, authenticated returns empty filtered list
        var response = await _noPermissionsClient.GetAsync("/api/v1/configurations", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

