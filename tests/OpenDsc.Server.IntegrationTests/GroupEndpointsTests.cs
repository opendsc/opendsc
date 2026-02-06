// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using FluentAssertions;

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
}
