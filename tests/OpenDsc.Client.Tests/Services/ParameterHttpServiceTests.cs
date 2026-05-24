// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using Xunit;

using OpenDsc.Client.Services;
using OpenDsc.Client.Tests.Helpers;
using OpenDsc.Contracts.Parameters;
using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Client.Tests.Services;

public sealed class ParameterHttpServiceTests
{
    private static ParameterHttpService CreateService(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new ParameterHttpService(client);
    }

    // ── GetVersionsAsync ──────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetVersionsAsync_Gets_Versions_Endpoint()
    {
        var scopeTypeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var versions = new List<ParameterVersionDetails> { new() { Version = "1.0.0" } };
        var handler = new FakeHttpMessageHandler().RespondOk(versions);
        var service = CreateService(handler);

        var result = await service.GetVersionsAsync(scopeTypeId, configId, null, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/parameters/{scopeTypeId}/{configId}/versions");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetVersionsAsync_With_ScopeValue_Appends_QueryString()
    {
        var scopeTypeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var versions = new List<ParameterVersionDetails>();
        var handler = new FakeHttpMessageHandler().RespondOk(versions);
        var service = CreateService(handler);

        await service.GetVersionsAsync(scopeTypeId, configId, "us-east", TestContext.Current.CancellationToken);

        handler.LastRequest!.RequestUri!.ToString().Should().Contain("scopeValue=us-east");
    }

    // ── GetNodeProvenanceAsync ────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodeProvenanceAsync_Gets_Provenance_Endpoint()
    {
        var nodeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var provenance = new ParameterProvenanceDetails();
        var handler = new FakeHttpMessageHandler().RespondOk(provenance);
        var service = CreateService(handler);

        var result = await service.GetNodeProvenanceAsync(nodeId, configId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().Contain($"api/v1/nodes/{nodeId}/parameters/provenance");
        handler.LastRequest.RequestUri!.ToString().Should().Contain($"configurationId={configId}");
    }

    // ── GetNodeResolutionAsync ────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodeResolutionAsync_Gets_Resolution_Endpoint()
    {
        var nodeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var resolution = new ParameterResolutionDetails();
        var handler = new FakeHttpMessageHandler().RespondOk(resolution);
        var service = CreateService(handler);

        var result = await service.GetNodeResolutionAsync(nodeId, configId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        handler.LastRequest!.RequestUri!.ToString().Should().Contain($"api/v1/nodes/{nodeId}/parameters/resolution");
        handler.LastRequest.RequestUri!.ToString().Should().Contain($"configurationId={configId}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetNodeResolutionAsync_Returns_Null_On_404()
    {
        var nodeId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().Respond(new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var result = await service.GetNodeResolutionAsync(nodeId, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().BeNull();
    }

    // ── GetMajorVersionSummariesAsync ─────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetMajorVersionSummariesAsync_Gets_Majors_Endpoint()
    {
        var scopeTypeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var summaries = new List<MajorVersionSummary> { new() { MajorVersion = 1 } };
        var handler = new FakeHttpMessageHandler().RespondOk(summaries);
        var service = CreateService(handler);

        var result = await service.GetMajorVersionSummariesAsync(scopeTypeId, configId, cancellationToken: TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/parameters/{scopeTypeId}/{configId}/majors");
    }

    // ── GetActiveParameterForMajorAsync ───────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetActiveParameterForMajorAsync_Gets_Major_Version_Endpoint()
    {
        var scopeTypeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var detail = new ParameterVersionDetails { Version = "1.2.0" };
        var handler = new FakeHttpMessageHandler().RespondOk(detail);
        var service = CreateService(handler);

        var result = await service.GetActiveParameterForMajorAsync(scopeTypeId, configId, 1, cancellationToken: TestContext.Current.CancellationToken);

        result!.Version.Should().Be("1.2.0");
        handler.LastRequest!.RequestUri!.ToString().Should().EndWith($"api/v1/parameters/{scopeTypeId}/{configId}/majors/1");
    }

    // ── CreateAsync ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateAsync_Puts_To_Parameters_Endpoint()
    {
        var scopeTypeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var created = new ParameterVersionDetails { Version = "1.0.0" };
        var handler = new FakeHttpMessageHandler().RespondOk(created);
        var service = CreateService(handler);

        var result = await service.CreateAsync(scopeTypeId, configId, new CreateParameterRequest(), TestContext.Current.CancellationToken);

        result.Version.Should().Be("1.0.0");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/parameters/{scopeTypeId}/{configId}");
    }

    // ── PublishAsync ──────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PublishAsync_Puts_To_Publish_Endpoint()
    {
        var scopeTypeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.PublishAsync(scopeTypeId, configId, null, "1.0.0", TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/parameters/{scopeTypeId}/{configId}/versions/1.0.0/publish");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task PublishAsync_With_ScopeValue_Appends_QueryString()
    {
        var scopeTypeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.PublishAsync(scopeTypeId, configId, "us-east", "1.0.0", TestContext.Current.CancellationToken);

        handler.LastRequest!.RequestUri!.ToString().Should().Contain("scopeValue=us-east");
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteAsync_Deletes_Parameter_Version()
    {
        var scopeTypeId = Guid.NewGuid();
        var configId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.DeleteAsync(scopeTypeId, configId, null, "1.0.0", TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/parameters/{scopeTypeId}/{configId}/versions/1.0.0");
    }

    // ── Additional endpoints ──────────────────────────────────────────────────

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetContentAsync_Gets_Parameter_Content_Endpoint()
    {
        var parameterId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondOk("parameters:\n  appName: TestApp");
        var service = CreateService(handler);

        var content = await service.GetContentAsync(parameterId, TestContext.Current.CancellationToken);

        content.Should().Contain("appName");
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/parameters/versions/{parameterId}/content");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAvailableMajorVersionsAsync_Gets_Available_Majors_Endpoint()
    {
        var configurationId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondOk(new List<int> { 1, 2 });
        var service = CreateService(handler);

        var majors = await service.GetAvailableMajorVersionsAsync(configurationId, TestContext.Current.CancellationToken);

        majors.Should().Contain(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/parameters/configurations/{configurationId}/available-majors");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task UpdateAsync_Puts_To_Parameter_Version_Endpoint()
    {
        var parameterId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.UpdateAsync(parameterId, new UpdateParameterRequest { Content = "parameters:\n  appName: Updated" }, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/parameters/versions/{parameterId}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetPermissionsAsync_Gets_Configuration_Permissions_Endpoint()
    {
        var configurationId = Guid.NewGuid();
        var permissions = new List<PermissionEntry>
        {
            new() { PrincipalType = PrincipalType.User, PrincipalId = Guid.NewGuid(), Level = ResourcePermission.Read }
        };
        var handler = new FakeHttpMessageHandler().RespondOk(permissions);
        var service = CreateService(handler);

        var result = await service.GetPermissionsAsync(configurationId, TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/parameters/configurations/{configurationId}/permissions");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GrantPermissionAsync_Puts_To_Configuration_Permissions_Endpoint()
    {
        var configurationId = Guid.NewGuid();
        var request = new GrantPermissionRequest
        {
            PrincipalType = PrincipalType.User,
            PrincipalId = Guid.NewGuid(),
            Level = ResourcePermission.Modify
        };
        var handler = new FakeHttpMessageHandler().RespondOk(new { });
        var service = CreateService(handler);

        await service.GrantPermissionAsync(configurationId, request, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/parameters/configurations/{configurationId}/permissions");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RevokePermissionAsync_Deletes_Configuration_Permissions_Endpoint()
    {
        var configurationId = Guid.NewGuid();
        var request = new RevokePermissionRequest
        {
            PrincipalType = PrincipalType.Group,
            PrincipalId = Guid.NewGuid()
        };
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.RevokePermissionAsync(configurationId, request, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith(
            $"api/v1/parameters/configurations/{configurationId}/permissions/{request.PrincipalType}/{request.PrincipalId}");
    }
}
