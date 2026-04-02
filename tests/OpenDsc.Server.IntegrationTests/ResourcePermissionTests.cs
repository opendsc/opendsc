// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Contracts;
using OpenDsc.Server.Data;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

/// <summary>
/// Tests that resource ACLs correctly restrict access to configurations and composite
/// configurations, and that the permission management endpoints (grant/revoke/list) work.
/// </summary>
[Trait("Category", "Integration")]
public class ResourcePermissionTests : IAsyncLifetime
{
    private readonly ServerWebApplicationFactory _factory = new();
    private HttpClient _adminClient = null!;

    public async ValueTask InitializeAsync()
    {
        _adminClient = await _factory.CreateAuthenticatedClientAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _adminClient?.Dispose();
        await _factory.DisposeAsync();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private async Task<string> CreateConfigurationAsync(string name)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "name");
        content.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var file = new ByteArrayContent("$schema: test\nresources: []"u8.ToArray());
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(file, "files", "main.dsc.yaml");

        var response = await _adminClient.PostAsync("/api/v1/configurations", content);
        response.EnsureSuccessStatusCode();
        return name;
    }

    private async Task<string> CreateCompositeConfigurationAsync(string name)
    {
        var response = await _adminClient.PostAsJsonAsync("/api/v1/composite-configurations",
            new { name, description = (string?)null });
        response.EnsureSuccessStatusCode();
        return name;
    }

    private async Task<Guid> GetTestUserId(string username)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        return user?.Id ?? throw new InvalidOperationException($"User '{username}' not found");
    }

    // ─── Configuration ACL Tests ──────────────────────────────────────────────

    [Fact]
    public async Task GetConfiguration_WithNoAcl_ReturnsForbidden()
    {
        var configName = $"acl-test-{Guid.NewGuid():N}";
        await CreateConfigurationAsync(configName);

        using var noPrivClient = await _factory.CreateUnprivilegedUserAsync($"noacl-cfg-{Guid.NewGuid():N}");

        var response = await noPrivClient.GetAsync($"/api/v1/configurations/{configName}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetConfiguration_WithReadAcl_ReturnsOk()
    {
        var configName = $"read-acl-{Guid.NewGuid():N}";
        await CreateConfigurationAsync(configName);

        var username = $"reader-{Guid.NewGuid():N}";
        using var readerClient = await _factory.CreateUnprivilegedUserAsync(username);
        var userId = await GetTestUserId(username);

        // Grant Read permission via admin
        var grantResponse = await _adminClient.PutAsJsonAsync(
            $"/api/v1/configurations/{configName}/permissions",
            new { principalType = "User", principalId = userId, level = "Read" }, TestContext.Current.CancellationToken);
        grantResponse.EnsureSuccessStatusCode();

        var getResponse = await readerClient.GetAsync($"/api/v1/configurations/{configName}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateConfiguration_WithReadOnlyAcl_ReturnsForbidden()
    {
        var configName = $"modify-deny-{Guid.NewGuid():N}";
        await CreateConfigurationAsync(configName);

        var username = $"readonly-{Guid.NewGuid():N}";
        using var readerClient = await _factory.CreateUnprivilegedUserAsync(username);
        var userId = await GetTestUserId(username);

        await _adminClient.PutAsJsonAsync(
            $"/api/v1/configurations/{configName}/permissions",
            new { principalType = "User", principalId = userId, level = "Read" }, TestContext.Current.CancellationToken);

        var patchResponse = await readerClient.PatchAsJsonAsync(
            $"/api/v1/configurations/{configName}",
            new { description = "hacked" }, TestContext.Current.CancellationToken);
        patchResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateConfiguration_WithModifyAcl_ReturnsOk()
    {
        var configName = $"modify-ok-{Guid.NewGuid():N}";
        await CreateConfigurationAsync(configName);

        var username = $"modifier-{Guid.NewGuid():N}";
        using var modifierClient = await _factory.CreateUnprivilegedUserAsync(username);
        var userId = await GetTestUserId(username);

        await _adminClient.PutAsJsonAsync(
            $"/api/v1/configurations/{configName}/permissions",
            new { principalType = "User", principalId = userId, level = "Modify" }, TestContext.Current.CancellationToken);

        var patchResponse = await modifierClient.PatchAsJsonAsync(
            $"/api/v1/configurations/{configName}",
            new { description = "updated" }, TestContext.Current.CancellationToken);
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── Configuration Permission Management Endpoints ────────────────────────

    [Fact]
    public async Task GetConfigurationPermissions_WithNoManageAcl_ReturnsForbidden()
    {
        var configName = $"perm-get-deny-{Guid.NewGuid():N}";
        await CreateConfigurationAsync(configName);

        var username = $"nomanage-{Guid.NewGuid():N}";
        using var noManageClient = await _factory.CreateUnprivilegedUserAsync(username);
        var userId = await GetTestUserId(username);

        // Give only Read permission (not Manage)
        await _adminClient.PutAsJsonAsync(
            $"/api/v1/configurations/{configName}/permissions",
            new { principalType = "User", principalId = userId, level = "Read" }, TestContext.Current.CancellationToken);

        var response = await noManageClient.GetAsync($"/api/v1/configurations/{configName}/permissions", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetConfigurationPermissions_AsAdmin_ReturnsAcl()
    {
        var configName = $"perm-get-ok-{Guid.NewGuid():N}";
        await CreateConfigurationAsync(configName);

        var username = $"grantee-{Guid.NewGuid():N}";
        using var noPrivClient = await _factory.CreateUnprivilegedUserAsync(username);
        var granteeId = await GetTestUserId(username);

        await _adminClient.PutAsJsonAsync(
            $"/api/v1/configurations/{configName}/permissions",
            new { principalType = "User", principalId = granteeId, level = "Read" }, TestContext.Current.CancellationToken);

        var response = await _adminClient.GetAsync($"/api/v1/configurations/{configName}/permissions", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var permissions = await response.Content.ReadFromJsonAsync<List<PermissionEntryDto>>(TestContext.Current.CancellationToken);
        permissions.Should().NotBeNullOrEmpty();
        permissions!.Should().ContainSingle(p => p.PrincipalId == granteeId && p.Level == "Read");
    }

    [Fact]
    public async Task GrantConfigurationPermission_WithInvalidLevel_ReturnsBadRequest()
    {
        var configName = $"perm-badlevel-{Guid.NewGuid():N}";
        await CreateConfigurationAsync(configName);

        var response = await _adminClient.PutAsJsonAsync(
            $"/api/v1/configurations/{configName}/permissions",
            new { principalType = "User", principalId = Guid.NewGuid(), level = "SuperAdmin" }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GrantConfigurationPermission_WithInvalidPrincipalType_ReturnsBadRequest()
    {
        var configName = $"perm-badtype-{Guid.NewGuid():N}";
        await CreateConfigurationAsync(configName);

        var response = await _adminClient.PutAsJsonAsync(
            $"/api/v1/configurations/{configName}/permissions",
            new { principalType = "Robot", principalId = Guid.NewGuid(), level = "Read" }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RevokeConfigurationPermission_AsAdmin_RemovesEntry()
    {
        var configName = $"perm-revoke-{Guid.NewGuid():N}";
        await CreateConfigurationAsync(configName);

        var username = $"revokee-{Guid.NewGuid():N}";
        using var granteeClient = await _factory.CreateUnprivilegedUserAsync(username);
        var granteeId = await GetTestUserId(username);

        // Grant
        await _adminClient.PutAsJsonAsync(
            $"/api/v1/configurations/{configName}/permissions",
            new { principalType = "User", principalId = granteeId, level = "Read" }, TestContext.Current.CancellationToken);

        // Confirm granted
        var canReadBefore = await granteeClient.GetAsync($"/api/v1/configurations/{configName}", TestContext.Current.CancellationToken);
        canReadBefore.StatusCode.Should().Be(HttpStatusCode.OK);

        // Revoke
        var revokeResponse = await _adminClient.DeleteAsync(
            $"/api/v1/configurations/{configName}/permissions/User/{granteeId}", TestContext.Current.CancellationToken);
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Confirm revoked
        var canReadAfter = await granteeClient.GetAsync($"/api/v1/configurations/{configName}", TestContext.Current.CancellationToken);
        canReadAfter.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── Composite Configuration ACL Tests ───────────────────────────────────

    [Fact]
    public async Task GetCompositeConfiguration_WithNoAcl_ReturnsForbidden()
    {
        var name = $"comp-acl-{Guid.NewGuid():N}";
        await CreateCompositeConfigurationAsync(name);

        using var noPrivClient = await _factory.CreateUnprivilegedUserAsync($"comp-nopriv-{Guid.NewGuid():N}");
        var response = await noPrivClient.GetAsync($"/api/v1/composite-configurations/{name}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetCompositeConfiguration_WithReadAcl_ReturnsOk()
    {
        var name = $"comp-read-{Guid.NewGuid():N}";
        await CreateCompositeConfigurationAsync(name);

        var username = $"comp-reader-{Guid.NewGuid():N}";
        using var readerClient = await _factory.CreateUnprivilegedUserAsync(username);
        var userId = await GetTestUserId(username);

        await _adminClient.PutAsJsonAsync(
            $"/api/v1/composite-configurations/{name}/permissions",
            new { principalType = "User", principalId = userId, level = "Read" }, TestContext.Current.CancellationToken);

        var response = await readerClient.GetAsync($"/api/v1/composite-configurations/{name}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ─── Composite Permission Management Endpoints ────────────────────────────

    [Fact]
    public async Task GetCompositePermissions_AsAdmin_ReturnsAcl()
    {
        var name = $"comp-perm-{Guid.NewGuid():N}";
        await CreateCompositeConfigurationAsync(name);

        var username = $"comp-grantee-{Guid.NewGuid():N}";
        using var granteeClient = await _factory.CreateUnprivilegedUserAsync(username);
        var granteeId = await GetTestUserId(username);

        await _adminClient.PutAsJsonAsync(
            $"/api/v1/composite-configurations/{name}/permissions",
            new { principalType = "User", principalId = granteeId, level = "Modify" }, TestContext.Current.CancellationToken);

        var response = await _adminClient.GetAsync($"/api/v1/composite-configurations/{name}/permissions", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var permissions = await response.Content.ReadFromJsonAsync<List<PermissionEntryDto>>(TestContext.Current.CancellationToken);
        permissions.Should().NotBeNullOrEmpty();
        permissions!.Should().ContainSingle(p => p.PrincipalId == granteeId && p.Level == "Modify");
    }

    [Fact]
    public async Task RevokeCompositePermission_AsAdmin_RemovesEntry()
    {
        var name = $"comp-revoke-{Guid.NewGuid():N}";
        await CreateCompositeConfigurationAsync(name);

        var username = $"comp-revokee-{Guid.NewGuid():N}";
        using var granteeClient = await _factory.CreateUnprivilegedUserAsync(username);
        var granteeId = await GetTestUserId(username);

        await _adminClient.PutAsJsonAsync(
            $"/api/v1/composite-configurations/{name}/permissions",
            new { principalType = "User", principalId = granteeId, level = "Read" }, TestContext.Current.CancellationToken);

        var canReadBefore = await granteeClient.GetAsync($"/api/v1/composite-configurations/{name}", TestContext.Current.CancellationToken);
        canReadBefore.StatusCode.Should().Be(HttpStatusCode.OK);

        var revokeResponse = await _adminClient.DeleteAsync(
            $"/api/v1/composite-configurations/{name}/permissions/User/{granteeId}", TestContext.Current.CancellationToken);
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var canReadAfter = await granteeClient.GetAsync($"/api/v1/composite-configurations/{name}", TestContext.Current.CancellationToken);
        canReadAfter.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── Group permission grants ──────────────────────────────────────────────

    [Fact]
    public async Task GrantConfigurationPermission_ToGroup_AllowsGroupMemberAccess()
    {
        var configName = $"group-acl-{Guid.NewGuid():N}";
        await CreateConfigurationAsync(configName);

        // Create a user to be in the group
        var username = $"group-member-{Guid.NewGuid():N}";
        using var memberClient = await _factory.CreateUnprivilegedUserAsync(username);
        var memberId = await GetTestUserId(username);

        // Create a group and add the user via admin endpoints
        var groupResponse = await _adminClient.PostAsJsonAsync("/api/v1/groups",
            new { name = $"TestGroup-{Guid.NewGuid():N}", description = (string?)null }, TestContext.Current.CancellationToken);
        groupResponse.EnsureSuccessStatusCode();

        var groupId = Guid.Parse(groupResponse.Headers.Location!.ToString().Split('/').Last());

        // SetGroupMembers uses a list of userIds
        var setMembersResponse = await _adminClient.PutAsJsonAsync($"/api/v1/groups/{groupId}/members",
            new { userIds = new[] { memberId } }, TestContext.Current.CancellationToken);
        setMembersResponse.EnsureSuccessStatusCode();

        // Grant Read permission to the group
        var grantResponse = await _adminClient.PutAsJsonAsync(
            $"/api/v1/configurations/{configName}/permissions",
            new { principalType = "Group", principalId = groupId, level = "Read" }, TestContext.Current.CancellationToken);
        grantResponse.EnsureSuccessStatusCode();

        // Member should now be able to read
        var readResponse = await memberClient.GetAsync($"/api/v1/configurations/{configName}", TestContext.Current.CancellationToken);
        readResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
