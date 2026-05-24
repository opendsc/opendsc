// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Text;

using AwesomeAssertions;

using Xunit;

using OpenDsc.Client.Services;
using OpenDsc.Client.Tests.Helpers;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Tests.Services;

public sealed class UserHttpServiceTests
{
    private static UserHttpService CreateService(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new UserHttpService(client);
    }

    // ── GetUsersAsync ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUsersAsync_Gets_Users_Endpoint()
    {
        var users = new List<UserSummary> { new() { Username = "alice" } };
        var handler = new FakeHttpMessageHandler().RespondOk(users);
        var service = CreateService(handler);

        var result = await service.GetUsersAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/users");
    }

    // ── GetUserAsync ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUserAsync_Gets_User_By_Id()
    {
        var userId = Guid.NewGuid();
        var user = new UserDetails { Id = userId, Username = "alice" };
        var handler = new FakeHttpMessageHandler().RespondOk(user);
        var service = CreateService(handler);

        var result = await service.GetUserAsync(userId, TestContext.Current.CancellationToken);

        result.Id.Should().Be(userId);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/users/{userId}");
    }

    // ── GetUserRolesAsync ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUserRolesAsync_Gets_Roles_Endpoint()
    {
        var userId = Guid.NewGuid();
        var roles = new List<RoleSummary> { new() { Name = "Admin" } };
        var handler = new FakeHttpMessageHandler().RespondOk(roles);
        var service = CreateService(handler);

        var result = await service.GetUserRolesAsync(userId, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/users/{userId}/roles");
    }

    // ── CreateUserAsync ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateUserAsync_Posts_To_Users_Endpoint()
    {
        var created = new UserSummary { Username = "bob" };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);

        var result = await service.CreateUserAsync(new CreateUserRequest { Username = "bob" }, TestContext.Current.CancellationToken);

        result.Username.Should().Be("bob");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/users");
    }

    // ── UpdateUserAsync ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateUserAsync_Puts_To_User_Endpoint()
    {
        var userId = Guid.NewGuid();
        var updated = new UserSummary { Id = userId };
        var handler = new FakeHttpMessageHandler().RespondOk(updated);
        var service = CreateService(handler);

        var result = await service.UpdateUserAsync(userId, new UpdateUserRequest(), TestContext.Current.CancellationToken);

        result.Id.Should().Be(userId);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/users/{userId}");
    }

    // ── DeleteUserAsync ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteUserAsync_Deletes_User()
    {
        var userId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.DeleteUserAsync(userId, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/users/{userId}");
    }

    // ── ResetPasswordAsync ────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ResetPasswordAsync_Posts_To_Reset_Endpoint()
    {
        var userId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.ResetPasswordAsync(userId, new ResetPasswordRequest { NewPassword = "new" }, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/users/{userId}/reset-password");
    }

    // ── UnlockUserAsync ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UnlockUserAsync_Posts_To_Unlock_Endpoint()
    {
        var userId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.UnlockUserAsync(userId, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/users/{userId}/unlock");
    }

    // ── SetUserRolesAsync ─────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task SetUserRolesAsync_Puts_To_Roles_Endpoint()
    {
        var userId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.SetUserRolesAsync(userId, new SetUserRolesRequest(), TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/users/{userId}/roles");
    }

    // ── Error mapping ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteUserAsync_Throws_KeyNotFoundException_On_404()
    {
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var act = async () => await service.DeleteUserAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUsersAsync_Throws_DscApiException_On_Unexpected_Status_Code()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            ReasonPhrase = "ServerError",
            Content = new StringContent("server exploded", Encoding.UTF8, "text/plain")
        };
        var handler = new FakeHttpMessageHandler().Respond(response);
        var service = CreateService(handler);

        var act = async () => await service.GetUsersAsync(TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<OpenDsc.Client.Http.DscApiException>();
        exception.Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        exception.Which.ResponseBody.Should().Be("server exploded");
        exception.Which.Message.Should().Contain("500");
    }

    // ── GetCurrentUserAsync ───────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetCurrentUserAsync_Gets_Current_User()
    {
        var user = new CurrentUserDetails { UserId = Guid.NewGuid(), Username = "alice" };
        var handler = new FakeHttpMessageHandler().RespondOk(user);
        var service = CreateService(handler);

        var result = await service.GetCurrentUserAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result!.Username.Should().Be("alice");
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/auth/me");
    }

    // ── GetCurrentUserAsync returns null on 404 ──────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetCurrentUserAsync_Returns_Null_On_404()
    {
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var result = await service.GetCurrentUserAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ── GetExternalLoginAsync ──────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetExternalLoginAsync_Gets_External_Login()
    {
        var handler = new FakeHttpMessageHandler().RespondOk("external-user");
        var service = CreateService(handler);
        var userId = Guid.NewGuid();

        var result = await service.GetExternalLoginAsync(userId, TestContext.Current.CancellationToken);

        result.Should().Be("external-user");
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/users/{userId}/external-login");
    }

    // ── GetExternalLoginAsync returns null on 404 ───────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetExternalLoginAsync_Returns_Null_On_404()
    {
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var result = await service.GetExternalLoginAsync(Guid.NewGuid(), TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ── GetUserRoleCountsAsync ────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUserRoleCountsAsync_Gets_Role_Counts()
    {
        var counts = new Dictionary<Guid, int> { { Guid.NewGuid(), 2 } };
        var handler = new FakeHttpMessageHandler().RespondOk(counts);
        var service = CreateService(handler);

        var result = await service.GetUserRoleCountsAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/users/counts/roles");
    }

    // ── GetUserGroupCountsAsync ───────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUserGroupCountsAsync_Gets_Group_Counts()
    {
        var counts = new Dictionary<Guid, int> { { Guid.NewGuid(), 3 } };
        var handler = new FakeHttpMessageHandler().RespondOk(counts);
        var service = CreateService(handler);

        var result = await service.GetUserGroupCountsAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/users/counts/groups");
    }

    // ── GetEffectivePermissionsAsync ───────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetEffectivePermissionsAsync_Gets_Permissions()
    {
        var userId = Guid.NewGuid();
        var permissions = new HashSet<string> { "read", "write" };
        var handler = new FakeHttpMessageHandler().RespondOk(permissions);
        var service = CreateService(handler);

        var result = await service.GetEffectivePermissionsAsync(userId, TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/users/{userId}/effective-permissions");
    }

    // ── ChangePasswordAsync ────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ChangePasswordAsync_Posts_To_Change_Password_Endpoint()
    {
        var userId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.ChangePasswordAsync(userId, new ChangePasswordRequest { CurrentPassword = "old", NewPassword = "new" }, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/auth/change-password");
    }

    // ── AssignRoleAsync ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task AssignRoleAsync_Posts_To_Roles_Endpoint()
    {
        var userId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.AssignRoleAsync(userId, new AssignRoleRequest { RoleId = Guid.NewGuid() }, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/users/{userId}/roles");
    }

    // ── RemoveRoleAsync ────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RemoveRoleAsync_Deletes_From_Roles_Endpoint()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.RemoveRoleAsync(userId, new RemoveRoleRequest { RoleId = roleId }, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/users/{userId}/roles/{roleId}");
    }
}
