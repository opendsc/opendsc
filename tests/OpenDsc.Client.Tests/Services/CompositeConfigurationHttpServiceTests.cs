// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using Xunit;

using OpenDsc.Client.Services;
using OpenDsc.Client.Tests.Helpers;
using OpenDsc.Contracts.CompositeConfigurations;
using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Client.Tests.Services;

public sealed class CompositeConfigurationHttpServiceTests
{
    private static CompositeConfigurationHttpService CreateService(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new CompositeConfigurationHttpService(client);
    }

    // ── GetCompositeConfigurationsAsync ───────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetCompositeConfigurationsAsync_Gets_Correct_Endpoint()
    {
        var list = new List<CompositeConfigurationSummary> { new() { Name = "bundle" } };
        var handler = new FakeHttpMessageHandler().RespondOk(list);
        var service = CreateService(handler);

        var result = await service.GetCompositeConfigurationsAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/composite-configurations");
    }

    // ── GetCompositeConfigurationAsync ────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetCompositeConfigurationAsync_Gets_Named_Endpoint()
    {
        var details = new CompositeConfigurationDetails { Name = "bundle" };
        var handler = new FakeHttpMessageHandler().RespondOk(details);
        var service = CreateService(handler);

        var result = await service.GetCompositeConfigurationAsync("bundle", TestContext.Current.CancellationToken);

        result!.Name.Should().Be("bundle");
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/composite-configurations/bundle");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetCompositeConfigurationAsync_Returns_Null_On_404()
    {
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var result = await service.GetCompositeConfigurationAsync("missing", TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ── GetVersionsAsync ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetVersionsAsync_Gets_Versions_Endpoint()
    {
        var versions = new List<CompositeConfigurationVersionDetails> { new() { Version = "1.0.0" } };
        var handler = new FakeHttpMessageHandler().RespondOk(versions);
        var service = CreateService(handler);

        var result = await service.GetVersionsAsync("bundle", TestContext.Current.CancellationToken);

        result!.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/composite-configurations/bundle/versions");
    }

    // ── GetVersionAsync ───────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetVersionAsync_Gets_Specific_Version_Endpoint()
    {
        var version = new CompositeConfigurationVersionDetails { Version = "1.0.0" };
        var handler = new FakeHttpMessageHandler().RespondOk(version);
        var service = CreateService(handler);

        var result = await service.GetVersionAsync("bundle", "1.0.0", TestContext.Current.CancellationToken);

        result!.Version.Should().Be("1.0.0");
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/composite-configurations/bundle/versions/1.0.0");
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateAsync_Posts_To_Correct_Endpoint()
    {
        var created = new CompositeConfigurationDetails { Name = "bundle" };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);

        var result = await service.CreateAsync(new CreateCompositeConfigurationRequest { Name = "bundle" }, TestContext.Current.CancellationToken);

        result.Name.Should().Be("bundle");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/composite-configurations");
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteAsync_Deletes_Named_Configuration()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.DeleteAsync("bundle", TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/composite-configurations/bundle");
    }

    // ── CreateVersionAsync ────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateVersionAsync_Posts_To_Version_Endpoint()
    {
        var created = new CompositeConfigurationVersionDetails { Version = "2.0.0" };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);

        var result = await service.CreateVersionAsync("bundle",
            new CreateCompositeConfigurationVersionRequest { Version = "2.0.0" },
            TestContext.Current.CancellationToken);

        result.Version.Should().Be("2.0.0");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/composite-configurations/bundle/versions");
    }

    // ── PublishVersionAsync ───────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PublishVersionAsync_Puts_To_Publish_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.PublishVersionAsync("bundle", "1.0.0", TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/composite-configurations/bundle/versions/1.0.0/publish");
    }

    // ── DeleteVersionAsync ────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteVersionAsync_Deletes_Version()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.DeleteVersionAsync("bundle", "1.0.0", TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/composite-configurations/bundle/versions/1.0.0");
    }

    // ── AddChildAsync ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task AddChildAsync_Posts_To_Children_Endpoint()
    {
        var childId = Guid.NewGuid();
        var created = new CompositeConfigurationItemDetails { Id = childId };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);

        var result = await service.AddChildAsync("bundle", "1.0.0",
            new AddChildConfigurationRequest(),
            TestContext.Current.CancellationToken);

        result.Id.Should().Be(childId);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/composite-configurations/bundle/versions/1.0.0/children");
    }

    // ── GetPermissionsAsync ───────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPermissionsAsync_Gets_Permissions_Endpoint()
    {
        var perms = new List<PermissionEntry> { new() { PrincipalType = PrincipalType.User } };
        var handler = new FakeHttpMessageHandler().RespondOk(perms);
        var service = CreateService(handler);

        var result = await service.GetPermissionsAsync("bundle", TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith("api/v1/composite-configurations/bundle/permissions");
    }

    // ── GrantPermissionAsync ──────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GrantPermissionAsync_Puts_To_Permissions_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.GrantPermissionAsync("bundle", new GrantPermissionRequest(), TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/composite-configurations/bundle/permissions");
    }

    // ── RevokePermissionAsync ─────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RevokePermissionAsync_Deletes_From_Permissions_Endpoint()
    {
        var principalId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.RevokePermissionAsync("bundle",
            new RevokePermissionRequest { PrincipalType = PrincipalType.Group, PrincipalId = principalId },
            TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString()
            .Should().EndWith($"api/v1/composite-configurations/bundle/permissions/Group/{principalId}");
    }

    // ── Error mapping ─────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteAsync_Throws_KeyNotFoundException_On_404()
    {
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var act = async () => await service.DeleteAsync("missing", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── Additional supported methods ─────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAvailableChildConfigurationsAsync_Gets_Available_Children_Endpoint()
    {
        var options = new List<ChildConfigurationOption> { new() { Name = "child" } };
        var handler = new FakeHttpMessageHandler().RespondOk(options);
        var service = CreateService(handler);
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var result = await service.GetAvailableChildConfigurationsAsync([id1, id2], TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        var uri = handler.LastRequest.RequestUri!.ToString();
        uri.Should().Contain("api/v1/composite-configurations/children/available?");
        uri.Should().Contain($"excludeIds={id1}");
        uri.Should().Contain($"excludeIds={id2}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAvailableMajorVersionsAsync_Gets_Major_Versions_Endpoint()
    {
        var majors = new List<int> { 1, 2 };
        var handler = new FakeHttpMessageHandler().RespondOk(majors);
        var service = CreateService(handler);
        var configurationId = Guid.NewGuid();

        var result = await service.GetAvailableMajorVersionsAsync(configurationId, TestContext.Current.CancellationToken);

        result.Should().BeEquivalentTo([1, 2]);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/composite-configurations/children/{configurationId}/major-versions");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateVersionFromExistingAsync_Posts_To_FromExisting_Endpoint()
    {
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.CreateVersionFromExistingAsync("bundle", new CreateCompositeVersionFromExistingRequest
        {
            SourceVersion = "1.0.0",
            NewVersion = "2.0.0"
        }, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/composite-configurations/bundle/versions/from-existing");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateChildAsync_Puts_To_Item_Endpoint()
    {
        var itemId = Guid.NewGuid();
        var updated = new CompositeConfigurationItemDetails { Id = itemId };
        var handler = new FakeHttpMessageHandler().RespondOk(updated);
        var service = CreateService(handler);

        var result = await service.UpdateChildAsync(itemId, new UpdateChildConfigurationRequest(), TestContext.Current.CancellationToken);

        result.Id.Should().Be(itemId);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/composite-configurations/children/{itemId}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RemoveChildAsync_Deletes_Item_Endpoint()
    {
        var itemId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.RemoveChildAsync(itemId, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/composite-configurations/children/{itemId}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task ReorderChildAsync_Puts_To_Reorder_Endpoint()
    {
        var itemId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.ReorderChildAsync(itemId, 3, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/composite-configurations/children/{itemId}/order/3");
    }
}
