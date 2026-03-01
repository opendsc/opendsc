// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Server.Endpoints;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class GroupEndpointsTests : IClassFixture<ServerWebApplicationFactory>
{
    private readonly ServerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public GroupEndpointsTests(ServerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetGroups_ReturnsGroupList()
    {
        var response = await _client.GetAsync("/api/v1/groups");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var groups = await response.Content.ReadFromJsonAsync<List<GroupSummaryDto>>();
        groups.Should().NotBeNull();
        groups!.Should().Contain(g => g.Name == "Administrators");
        groups.Should().Contain(g => g.Name == "Operators");
    }

    [Fact]
    public async Task GetGroup_WithValidId_ReturnsGroupDetails()
    {
        // Get the admin group
        var listResponse = await _client.GetAsync("/api/v1/groups");
        var groups = await listResponse.Content.ReadFromJsonAsync<List<GroupSummaryDto>>();
        var adminGroup = groups!.First(g => g.Name == "Administrators");

        var response = await _client.GetAsync($"/api/v1/groups/{adminGroup.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var group = await response.Content.ReadFromJsonAsync<GroupDetailDto>();
        group.Should().NotBeNull();
        group!.Name.Should().Be("Administrators");
        group.Description.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateGroup_WithValidData_CreatesGroup()
    {
        var createRequest = new CreateGroupRequest
        {
            Name = "TestGroup",
            Description = "A test group"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/groups", createRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var group = await response.Content.ReadFromJsonAsync<GroupDetailDto>();
        group.Should().NotBeNull();
        group!.Name.Should().Be("TestGroup");
        group.Description.Should().Be("A test group");
    }

    [Fact]
    public async Task UpdateGroup_WithValidData_UpdatesGroup()
    {
        // Create a test group first
        var createRequest = new CreateGroupRequest
        {
            Name = "UpdateGroup",
            Description = "Group to update"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/groups", createRequest);
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<GroupDetailDto>();
        createdGroup.Should().NotBeNull();

        // Update the group
        var updateRequest = new UpdateGroupRequest
        {
            Name = createdGroup!.Name,
            Description = "Updated group description"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/v1/groups/{createdGroup.Id}", updateRequest);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedGroup = await updateResponse.Content.ReadFromJsonAsync<GroupDetailDto>();
        updatedGroup.Should().NotBeNull();
        updatedGroup!.Description.Should().Be("Updated group description");
    }

    [Fact]
    public async Task DeleteGroup_WithValidId_DeletesGroup()
    {
        // Create a test group first
        var createRequest = new CreateGroupRequest
        {
            Name = "DeleteGroup",
            Description = "Group to delete"
        };
        var createResponse = await _client.PostAsJsonAsync("/api/v1/groups", createRequest);
        var createdGroup = await createResponse.Content.ReadFromJsonAsync<GroupDetailDto>();

        // Delete the group
        var deleteResponse = await _client.DeleteAsync($"/api/v1/groups/{createdGroup!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify group is gone
        var getResponse = await _client.GetAsync($"/api/v1/groups/{createdGroup.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetGroupMembers_WithValidGroup_ReturnsGroupMembers()
    {
        // Get the admin group
        var listResponse = await _client.GetAsync("/api/v1/groups");
        var groups = await listResponse.Content.ReadFromJsonAsync<List<GroupSummaryDto>>();
        var adminGroup = groups!.First(g => g.Name == "Administrators");

        var response = await _client.GetAsync($"/api/v1/groups/{adminGroup.Id}/members");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var members = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        members.Should().NotBeNull();
        members!.Should().Contain(u => u.Username == "admin");
    }

    [Fact]
    public async Task SetGroupMembers_WithValidData_SetsGroupMembers()
    {
        // Create a test group first
        var createGroupRequest = new CreateGroupRequest
        {
            Name = "MemberGroup",
            Description = "Group for member testing"
        };
        var createGroupResponse = await _client.PostAsJsonAsync("/api/v1/groups", createGroupRequest);
        var createdGroup = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>();

        // Create a test user
        var createUserRequest = new CreateUserRequest
        {
            Username = "memberuser",
            Email = "member@example.com",
            Password = "TestPassword123!"
        };
        var createUserResponse = await _client.PostAsJsonAsync("/api/v1/users", createUserRequest);
        var createdUser = await createUserResponse.Content.ReadFromJsonAsync<UserDto>();

        // Set group members
        var setMembersRequest = new SetMembersRequest
        {
            UserIds = [createdUser!.Id]
        };
        var setResponse = await _client.PutAsJsonAsync($"/api/v1/groups/{createdGroup!.Id}/members", setMembersRequest);

        setResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify members were set
        var getMembersResponse = await _client.GetAsync($"/api/v1/groups/{createdGroup.Id}/members");
        var members = await getMembersResponse.Content.ReadFromJsonAsync<List<UserDto>>();
        members.Should().NotBeNull();
        members!.Should().Contain(u => u.Username == "memberuser");
    }

    [Fact]
    public async Task GetGroupRoles_WithValidGroup_ReturnsGroupRoles()
    {
        // Get the admin group
        var listResponse = await _client.GetAsync("/api/v1/groups");
        var groups = await listResponse.Content.ReadFromJsonAsync<List<GroupSummaryDto>>();
        var adminGroup = groups!.First(g => g.Name == "Administrators");

        var response = await _client.GetAsync($"/api/v1/groups/{adminGroup.Id}/roles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var roles = await response.Content.ReadFromJsonAsync<List<RoleDto>>();
        roles.Should().NotBeNull();
        roles!.Should().Contain(r => r.Name == "Administrator");
    }

    [Fact]
    public async Task SetGroupRoles_WithValidData_SetsGroupRoles()
    {
        // Create a test group first
        var createGroupRequest = new CreateGroupRequest
        {
            Name = "RoleGroup",
            Description = "Group for role testing"
        };
        var createGroupResponse = await _client.PostAsJsonAsync("/api/v1/groups", createGroupRequest);
        var createdGroup = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>();

        // Get a role to assign
        var rolesResponse = await _client.GetAsync("/api/v1/roles");
        var roles = await rolesResponse.Content.ReadFromJsonAsync<List<RoleSummaryDto>>();
        var viewerRole = roles!.First(r => r.Name == "Viewer");

        // Set group roles
        var setRolesRequest = new SetRolesRequest
        {
            RoleIds = [viewerRole.Id]
        };
        var setResponse = await _client.PutAsJsonAsync($"/api/v1/groups/{createdGroup!.Id}/roles", setRolesRequest);

        setResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify roles were set
        var getRolesResponse = await _client.GetAsync($"/api/v1/groups/{createdGroup.Id}/roles");
        var groupRoles = await getRolesResponse.Content.ReadFromJsonAsync<List<RoleDto>>();
        groupRoles.Should().NotBeNull();
        groupRoles!.Should().Contain(r => r.Name == "Viewer");
    }

    [Fact]
    public async Task GetExternalGroupMappings_ReturnsExternalGroupMappings()
    {
        var response = await _client.GetAsync("/api/v1/groups/external-mappings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var mappings = await response.Content.ReadFromJsonAsync<List<ExternalGroupMappingDto>>();
        mappings.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateExternalGroupMapping_WithValidData_CreatesMapping()
    {
        // Create a test group first
        var createGroupRequest = new CreateGroupRequest
        {
            Name = "ExternalGroup",
            Description = "Group for external mapping"
        };
        var createGroupResponse = await _client.PostAsJsonAsync("/api/v1/groups", createGroupRequest);
        var createdGroup = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>();

        var createMappingRequest = new CreateExternalGroupMappingRequest
        {
            Provider = "TestProvider",
            ExternalGroupId = "external-group-123",
            ExternalGroupName = "Test External Group",
            GroupId = createdGroup!.Id
        };

        var response = await _client.PostAsJsonAsync("/api/v1/groups/external-mappings", createMappingRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var mapping = await response.Content.ReadFromJsonAsync<ExternalGroupMappingDto>();
        mapping.Should().NotBeNull();
        mapping!.ExternalGroupId.Should().Be("external-group-123");
        mapping.GroupId.Should().Be(createdGroup.Id);
    }

    [Fact]
    public async Task DeleteExternalGroupMapping_WithValidId_DeletesMapping()
    {
        // Create a test group first
        var createGroupRequest = new CreateGroupRequest
        {
            Name = "DeleteExternalGroup",
            Description = "Group for external mapping deletion"
        };
        var createGroupResponse = await _client.PostAsJsonAsync("/api/v1/groups", createGroupRequest);
        var createdGroup = await createGroupResponse.Content.ReadFromJsonAsync<GroupDetailDto>();

        // Create external mapping
        var createMappingRequest = new CreateExternalGroupMappingRequest
        {
            Provider = "TestProvider",
            ExternalGroupId = "external-group-delete",
            ExternalGroupName = "Test External Group for Deletion",
            GroupId = createdGroup!.Id
        };
        var createMappingResponse = await _client.PostAsJsonAsync("/api/v1/groups/external-mappings", createMappingRequest);
        var createdMapping = await createMappingResponse.Content.ReadFromJsonAsync<ExternalGroupMappingDto>();

        // Delete the mapping
        var deleteResponse = await _client.DeleteAsync($"/api/v1/groups/external-mappings/{createdMapping!.Id}");

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify mapping is gone (would need to check the list or specific endpoint if available)
    }
}
