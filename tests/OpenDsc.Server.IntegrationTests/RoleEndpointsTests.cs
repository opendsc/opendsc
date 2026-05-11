// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Contracts.Users;

using OpenDsc.Server.Endpoints;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class RoleEndpointsTests : IClassFixture<ServerWebApplicationFactory>
{
    private readonly ServerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RoleEndpointsTests(ServerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetRoles_ReturnsRoleList()
    {
        var response = await _client.GetAsync("/api/v1/roles", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<List<RoleSummary>>(TestContext.Current.CancellationToken);
        roles.Should().NotBeNull();
        roles!.Should().Contain(r => r.Name == "Administrator");
        roles.Should().Contain(r => r.Name == "Operator");
        roles.Should().Contain(r => r.Name == "Viewer");
    }

    [Fact]
    public async Task GetRole_WithValidId_ReturnsRoleDetails()
    {
        // Get the admin role
        var listResponse = await _client.GetAsync("/api/v1/roles", TestContext.Current.CancellationToken);
        var roles = await listResponse.Content.ReadFromJsonAsync<List<RoleSummary>>(TestContext.Current.CancellationToken);
        var adminRole = roles!.First(r => r.Name == "Administrator");

        var response = await _client.GetAsync($"/api/v1/roles/{adminRole.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var role = await response.Content.ReadFromJsonAsync<RoleDetails>(TestContext.Current.CancellationToken);
        role.Should().NotBeNull();
        role!.Name.Should().Be("Administrator");
        role.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateRole_WithValidData_CreatesRole()
    {
        var createRequest = new CreateRoleRequest
        {
            Name = "TestRole",
            Description = "A test role",
            Permissions = ["read", "write"]
        };

        var response = await _client.PostAsJsonAsync("/api/v1/roles", createRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var role = await response.Content.ReadFromJsonAsync<RoleSummary>(TestContext.Current.CancellationToken);
        role.Should().NotBeNull();
        role!.Name.Should().Be("TestRole");
        role.Description.Should().Be("A test role");
    }

    [Fact]
    public async Task UpdateRole_WithValidData_UpdatesRole()
    {
        // Create a test role first
        var createRequest = new CreateRoleRequest
        {
            Name = "UpdateRole",
            Description = "Role to update",
            Permissions = ["read"]
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/roles", createRequest, TestContext.Current.CancellationToken);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleSummary>(TestContext.Current.CancellationToken);

        // Update the role
        var updateRequest = new UpdateRoleRequest
        {
            Name = "UpdateRole",
            Description = "Updated role description",
            Permissions = ["read", "write"]
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/roles/{createdRole!.Id}", updateRequest, TestContext.Current.CancellationToken);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedRole = await updateResponse.Content.ReadFromJsonAsync<RoleSummary>(TestContext.Current.CancellationToken);
        updatedRole.Should().NotBeNull();
        updatedRole!.Description.Should().Be("Updated role description");
    }

    [Fact]
    public async Task DeleteRole_WithValidId_DeletesRole()
    {
        // Create a test role first
        var createRequest = new CreateRoleRequest
        {
            Name = "DeleteRole",
            Description = "Role to delete",
            Permissions = ["read"]
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/roles", createRequest, TestContext.Current.CancellationToken);
        var createdRole = await createResponse.Content.ReadFromJsonAsync<RoleSummary>(TestContext.Current.CancellationToken);

        // Delete the role
        var deleteResponse = await _client.DeleteAsync($"/api/v1/roles/{createdRole!.Id}", TestContext.Current.CancellationToken);

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify role is gone
        var getResponse = await _client.GetAsync($"/api/v1/roles/{createdRole.Id}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
