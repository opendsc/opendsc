// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Server.Endpoints;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class UserEndpointsTests : IClassFixture<ServerWebApplicationFactory>
{
    private readonly ServerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UserEndpointsTests(ServerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsUserList()
    {
        var response = await _client.GetAsync("/api/v1/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNull();
        users!.Should().Contain(u => u.Username == "admin");
    }

    [Fact]
    public async Task GetUser_WithValidId_ReturnsUserDetails()
    {
        // Get the admin user
        var listResponse = await _client.GetAsync("/api/v1/users");
        var users = await listResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        var adminUser = users!.First(u => u.Username == "admin");

        var response = await _client.GetAsync($"/api/v1/users/{adminUser.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDetailDto>();
        user.Should().NotBeNull();
        user!.Username.Should().Be("admin");
        user.Email.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateUser_WithValidData_CreatesUser()
    {
        var createRequest = new CreateUserRequest
        {
            Username = "testuser",
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/users", createRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Username.Should().Be("testuser");
        user.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task UpdateUser_WithValidData_UpdatesUser()
    {
        // Create a test user first
        var createRequest = new CreateUserRequest
        {
            Username = "updateuser",
            Email = "update@example.com",
            Password = "TestPassword123!"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();
        createdUser.Should().NotBeNull();

        // Update the user
        var updateRequest = new UpdateUserRequest
        {
            Username = createdUser!.Username,
            Email = "updated@example.com",
            IsActive = true
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/users/{createdUser.Id}", updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedUser = await updateResponse.Content.ReadFromJsonAsync<UserDto>();
        updatedUser.Should().NotBeNull();
        updatedUser!.Email.Should().Be("updated@example.com");
    }

    [Fact]
    public async Task DeleteUser_WithValidId_DeletesUser()
    {
        // Create a test user first
        var createRequest = new CreateUserRequest
        {
            Username = "deleteuser",
            Email = "delete@example.com",
            Password = "TestPassword123!"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Delete the user
        var deleteResponse = await _client.DeleteAsync($"/api/v1/users/{createdUser!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify user is gone
        var getResponse = await _client.GetAsync($"/api/v1/users/{createdUser.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResetPassword_WithValidUser_ResetsPassword()
    {
        // Create a test user first
        var createRequest = new CreateUserRequest
        {
            Username = "resetuser",
            Email = "reset@example.com",
            Password = "TestPassword123!"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Reset password
        var resetRequest = new { NewPassword = "NewTestPassword123!" };
        var resetResponse = await _client.PostAsJsonAsync($"/api/v1/users/{createdUser!.Id}/reset-password", resetRequest);

        resetResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UnlockUser_WithValidUser_UnlocksUser()
    {
        // Create a test user first
        var createRequest = new CreateUserRequest
        {
            Username = "lockuser",
            Email = "lock@example.com",
            Password = "TestPassword123!"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Unlock user (should work even if not locked)
        var unlockResponse = await _client.PostAsync($"/api/v1/users/{createdUser!.Id}/unlock", null);

        unlockResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUserRoles_WithValidUser_ReturnsUserRoles()
    {
        // Get the admin user
        var listResponse = await _client.GetAsync("/api/v1/users");
        var users = await listResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        var adminUser = users!.First(u => u.Username == "admin");

        var response = await _client.GetAsync($"/api/v1/users/{adminUser.Id}/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>();
        roles.Should().NotBeNull();
        roles!.Should().Contain(r => r.Name == "Administrator");
    }

    [Fact]
    public async Task SetUserRoles_WithValidData_SetsUserRoles()
    {
        // Create a test user first
        var createRequest = new CreateUserRequest
        {
            Username = "roleuser",
            Email = "role@example.com",
            Password = "TestPassword123!"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/users", createRequest);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        // Get a role to assign
        var rolesResponse = await _client.GetAsync("/api/v1/roles");
        var roles = await rolesResponse.Content.ReadFromJsonAsync<List<RoleSummaryDto>>();
        var viewerRole = roles!.First(r => r.Name == "Viewer");

        // Set user roles
        var setRolesRequest = new SetRolesRequest
        {
            RoleIds = [viewerRole.Id]
        };
        var setResponse = await _client.PutAsJsonAsync($"/api/v1/users/{createdUser!.Id}/roles", setRolesRequest);

        setResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify roles were set
        var getRolesResponse = await _client.GetAsync($"/api/v1/users/{createdUser.Id}/roles");
        var userRoles = await getRolesResponse.Content.ReadFromJsonAsync<List<RoleDto>>();
        userRoles.Should().NotBeNull();
        userRoles!.Should().Contain(r => r.Name == "Viewer");
    }
}
