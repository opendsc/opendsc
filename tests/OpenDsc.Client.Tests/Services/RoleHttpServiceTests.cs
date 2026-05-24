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

public sealed class RoleHttpServiceTests
{
    private static RoleHttpService CreateService(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new RoleHttpService(client);
    }

    // ── GetRolesAsync ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetRolesAsync_Gets_Roles_Endpoint()
    {
        var roles = new List<RoleSummary> { new() { Name = "Admin" } };
        var handler = new FakeHttpMessageHandler().RespondOk(roles);
        var service = CreateService(handler);

        var result = await service.GetRolesAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/roles");
    }

    // ── GetRoleAsync ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetRoleAsync_Gets_Role_By_Id()
    {
        var roleId = Guid.NewGuid();
        var role = new RoleDetails { Id = roleId, Name = "Admin" };
        var handler = new FakeHttpMessageHandler().RespondOk(role);
        var service = CreateService(handler);

        var result = await service.GetRoleAsync(roleId, TestContext.Current.CancellationToken);

        result.Id.Should().Be(roleId);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/roles/{roleId}");
    }

    // ── CreateRoleAsync ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateRoleAsync_Posts_To_Roles_Endpoint()
    {
        var created = new RoleSummary { Name = "Viewer" };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);

        var result = await service.CreateRoleAsync(new CreateRoleRequest { Name = "Viewer" }, TestContext.Current.CancellationToken);

        result.Name.Should().Be("Viewer");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/roles");
    }

    // ── UpdateRoleAsync ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateRoleAsync_Puts_To_Role_Endpoint()
    {
        var roleId = Guid.NewGuid();
        var updated = new RoleSummary { Id = roleId };
        var handler = new FakeHttpMessageHandler().RespondOk(updated);
        var service = CreateService(handler);

        var result = await service.UpdateRoleAsync(roleId, new UpdateRoleRequest(), TestContext.Current.CancellationToken);

        result.Id.Should().Be(roleId);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/roles/{roleId}");
    }

    // ── DeleteRoleAsync ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteRoleAsync_Deletes_Role()
    {
        var roleId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.DeleteRoleAsync(roleId, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/roles/{roleId}");
    }

    // ── Error mapping ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteRoleAsync_Throws_KeyNotFoundException_On_404()
    {
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var act = async () => await service.DeleteRoleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── GetGroupsForRoleAsync ─────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetGroupsForRoleAsync_Gets_Groups_For_Role()
    {
        var roleId = Guid.NewGuid();
        var groups = new List<GroupSummary> { new() { Name = "Admins" } };
        var handler = new FakeHttpMessageHandler().RespondOk(groups);
        var service = CreateService(handler);

        var result = await service.GetGroupsForRoleAsync(roleId, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/roles/{roleId}/groups");
    }

    // ── GetGroupsForRoleAsync returns null on 404 ──────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetGroupsForRoleAsync_Returns_Null_On_404()
    {
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var result = await service.GetGroupsForRoleAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ── GetRoleUserCountsAsync ────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetRoleUserCountsAsync_Gets_User_Counts()
    {
        var counts = new Dictionary<Guid, int> { { Guid.NewGuid(), 10 } };
        var handler = new FakeHttpMessageHandler().RespondOk(counts);
        var service = CreateService(handler);

        var result = await service.GetRoleUserCountsAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/roles/counts/users");
    }

    // ── GetRoleGroupCountsAsync ───────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetRoleGroupCountsAsync_Gets_Group_Counts()
    {
        var counts = new Dictionary<Guid, int> { { Guid.NewGuid(), 5 } };
        var handler = new FakeHttpMessageHandler().RespondOk(counts);
        var service = CreateService(handler);

        var result = await service.GetRoleGroupCountsAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/roles/counts/groups");
    }

    // ── SetGroupsForRoleAsync ─────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SetGroupsForRoleAsync_Puts_To_Groups_Endpoint()
    {
        var roleId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.SetGroupsForRoleAsync(roleId, new SetRoleGroupsRequest(), TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/roles/{roleId}/groups");
    }
}
