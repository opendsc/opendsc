// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using Xunit;

using OpenDsc.Client.Services;
using OpenDsc.Client.Tests.Helpers;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Tests.Services;

public sealed class GroupHttpServiceTests
{
    private static GroupHttpService CreateService(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new GroupHttpService(client);
    }

    // GetGroupsAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetGroupsAsync_Gets_Groups_Endpoint()
    {
        var groups = new List<GroupSummary> { new() { Name = "admins" } };
        var handler = new FakeHttpMessageHandler().RespondOk(groups);
        var service = CreateService(handler);
        var result = await service.GetGroupsAsync(TestContext.Current.CancellationToken);
        result.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/groups");
    }

    // GetGroupAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetGroupAsync_Gets_Group_By_Id()
    {
        var groupId = Guid.NewGuid();
        var group = new GroupDetails { Id = groupId };
        var handler = new FakeHttpMessageHandler().RespondOk(group);
        var service = CreateService(handler);
        var result = await service.GetGroupAsync(groupId, TestContext.Current.CancellationToken);
        result.Id.Should().Be(groupId);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/groups/{groupId}");
    }

    // GetGroupMembersAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetGroupMembersAsync_Gets_Members_Endpoint()
    {
        var groupId = Guid.NewGuid();
        var members = new List<UserSummary> { new() { Username = "alice" } };
        var handler = new FakeHttpMessageHandler().RespondOk(members);
        var service = CreateService(handler);
        var result = await service.GetGroupMembersAsync(groupId, TestContext.Current.CancellationToken);
        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/groups/{groupId}/members");
    }

    // GetGroupRolesAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetGroupRolesAsync_Gets_Roles_Endpoint()
    {
        var groupId = Guid.NewGuid();
        var roles = new List<RoleSummary> { new() { Name = "Admin" } };
        var handler = new FakeHttpMessageHandler().RespondOk(roles);
        var service = CreateService(handler);
        var result = await service.GetGroupRolesAsync(groupId, TestContext.Current.CancellationToken);
        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/groups/{groupId}/roles");
    }

    // GetExternalGroupMappingsAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetExternalGroupMappingsAsync_Gets_Mappings_Endpoint()
    {
        var mappings = new List<ExternalGroupMappingInfo> { new() { ExternalGroupId = "ext-group" } };
        var handler = new FakeHttpMessageHandler().RespondOk(mappings);
        var service = CreateService(handler);
        var result = await service.GetExternalGroupMappingsAsync(TestContext.Current.CancellationToken);
        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/groups/external-mappings");
    }

    // CreateGroupAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateGroupAsync_Posts_To_Groups_Endpoint()
    {
        var created = new GroupSummary { Name = "devs" };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);
        var result = await service.CreateGroupAsync(new CreateGroupRequest { Name = "devs" }, TestContext.Current.CancellationToken);
        result.Name.Should().Be("devs");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/groups");
    }

    // UpdateGroupAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateGroupAsync_Puts_To_Group_Endpoint()
    {
        var groupId = Guid.NewGuid();
        var updated = new GroupSummary { Id = groupId };
        var handler = new FakeHttpMessageHandler().RespondOk(updated);
        var service = CreateService(handler);
        var result = await service.UpdateGroupAsync(groupId, new UpdateGroupRequest(), TestContext.Current.CancellationToken);
        result.Id.Should().Be(groupId);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/groups/{groupId}");
    }

    // DeleteGroupAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteGroupAsync_Deletes_Group()
    {
        var groupId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);
        await service.DeleteGroupAsync(groupId, TestContext.Current.CancellationToken);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/groups/{groupId}");
    }

    // SetMembersAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SetMembersAsync_Puts_To_Members_Endpoint()
    {
        var groupId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);
        await service.SetMembersAsync(groupId, new SetGroupMembersRequest(), TestContext.Current.CancellationToken);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/groups/{groupId}/members");
    }

    // SetRolesAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task SetRolesAsync_Puts_To_Roles_Endpoint()
    {
        var groupId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);
        await service.SetRolesAsync(groupId, new SetGroupRolesRequest(), TestContext.Current.CancellationToken);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/groups/{groupId}/roles");
    }

    // CreateExternalGroupMappingAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateExternalGroupMappingAsync_Posts_To_Mappings_Endpoint()
    {
        var created = new ExternalGroupMappingInfo { ExternalGroupId = "ext-1" };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);
        var result = await service.CreateExternalGroupMappingAsync(new CreateExternalGroupMappingRequest(), TestContext.Current.CancellationToken);
        result!.ExternalGroupId.Should().Be("ext-1");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/groups/external-mappings");
    }

    // DeleteExternalGroupMappingAsync
    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteExternalGroupMappingAsync_Deletes_Mapping()
    {
        var mappingId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);
        await service.DeleteExternalGroupMappingAsync(mappingId, TestContext.Current.CancellationToken);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/groups/external-mappings/{mappingId}");
    }

    // Error mapping
    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteGroupAsync_Throws_KeyNotFoundException_On_404()
    {
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);
        var act = async () => await service.DeleteGroupAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── AddMemberAsync ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task AddMemberAsync_Posts_To_Members_Endpoint()
    {
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.AddMemberAsync(groupId, new AddGroupMemberRequest { UserId = userId }, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/groups/{groupId}/members");
    }

    // ── RemoveMemberAsync ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RemoveMemberAsync_Deletes_From_Members_Endpoint()
    {
        var groupId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.RemoveMemberAsync(groupId, new RemoveGroupMemberRequest { UserId = userId }, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/groups/{groupId}/members/{userId}");
    }

    // ── AssignRoleAsync ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task AssignRoleAsync_Posts_To_Roles_Endpoint()
    {
        var groupId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.AssignRoleAsync(groupId, new AssignGroupRoleRequest { RoleId = roleId }, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/groups/{groupId}/roles");
    }

    // ── RemoveRoleAsync ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RemoveRoleAsync_Deletes_From_Roles_Endpoint()
    {
        var groupId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.RemoveRoleAsync(groupId, new RemoveGroupRoleRequest { RoleId = roleId }, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/groups/{groupId}/roles/{roleId}");
    }

    // ── GetGroupMemberCountsAsync ──────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetGroupMemberCountsAsync_Gets_Member_Counts()
    {
        var counts = new Dictionary<Guid, int> { { Guid.NewGuid(), 5 } };
        var handler = new FakeHttpMessageHandler().RespondOk(counts);
        var service = CreateService(handler);

        var result = await service.GetGroupMemberCountsAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/groups/counts/members");
    }

    // ── GetGroupRoleCountsAsync ────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetGroupRoleCountsAsync_Gets_Role_Counts()
    {
        var counts = new Dictionary<Guid, int> { { Guid.NewGuid(), 2 } };
        var handler = new FakeHttpMessageHandler().RespondOk(counts);
        var service = CreateService(handler);

        var result = await service.GetGroupRoleCountsAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/groups/counts/roles");
    }
}
