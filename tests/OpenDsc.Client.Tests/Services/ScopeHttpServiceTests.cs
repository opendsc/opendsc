// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using Xunit;

using OpenDsc.Client.Services;
using OpenDsc.Client.Tests.Helpers;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Tests.Services;

public sealed class ScopeHttpServiceTests
{
    private static ScopeHttpService CreateService(FakeHttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost/") };
        return new ScopeHttpService(client);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateScopeTypeAsync_Posts_To_Correct_Endpoint()
    {
        var created = new ScopeTypeDetails { Id = Guid.NewGuid(), Name = "Region" };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);

        var result = await service.CreateScopeTypeAsync(new CreateScopeTypeRequest { Name = "Region" }, TestContext.Current.CancellationToken);

        result.Id.Should().Be(created.Id);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/scope-types");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteScopeTypeAsync_Deletes_To_Correct_Endpoint()
    {
        var id = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.DeleteScopeTypeAsync(id, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/scope-types/{id}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task EnableScopeTypeAsync_Patches_Enable_Endpoint()
    {
        var id = Guid.NewGuid();
        var updated = new ScopeTypeDetails { Id = id, Name = "Region", IsEnabled = true };
        var handler = new FakeHttpMessageHandler().RespondOk(updated);
        var service = CreateService(handler);

        var result = await service.EnableScopeTypeAsync(id, TestContext.Current.CancellationToken);

        result.IsEnabled.Should().BeTrue();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Patch);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/scope-types/{id}/enable");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DisableScopeTypeAsync_Patches_Disable_Endpoint()
    {
        var id = Guid.NewGuid();
        var updated = new ScopeTypeDetails { Id = id, Name = "Region", IsEnabled = false };
        var handler = new FakeHttpMessageHandler().RespondOk(updated);
        var service = CreateService(handler);

        var result = await service.DisableScopeTypeAsync(id, TestContext.Current.CancellationToken);

        result.IsEnabled.Should().BeFalse();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Patch);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/scope-types/{id}/disable");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task CreateScopeValueAsync_Posts_To_Nested_Endpoint()
    {
        var scopeTypeId = Guid.NewGuid();
        var created = new ScopeValueDetails { Id = Guid.NewGuid() };
        var handler = new FakeHttpMessageHandler().RespondJson(HttpStatusCode.Created, created);
        var service = CreateService(handler);

        var result = await service.CreateScopeValueAsync(scopeTypeId, new CreateScopeValueRequest { Value = "us-east" }, TestContext.Current.CancellationToken);

        result.Id.Should().Be(created.Id);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Post);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/scope-types/{scopeTypeId}/values");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task DeleteScopeValueAsync_Deletes_To_Correct_Endpoint()
    {
        var scopeTypeId = Guid.NewGuid();
        var valueId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondNoContent();
        var service = CreateService(handler);

        await service.DeleteScopeValueAsync(scopeTypeId, valueId, TestContext.Current.CancellationToken);

        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.LastRequest.RequestUri!.ToString()
            .Should().EndWith($"api/v1/scope-types/{scopeTypeId}/values/{valueId}");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetScopeTypeUsageCountAsync_Gets_Usage_Count_Endpoint()
    {
        var scopeTypeId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondOk(3);
        var service = CreateService(handler);

        var result = await service.GetScopeTypeUsageCountAsync(scopeTypeId, TestContext.Current.CancellationToken);

        result.Should().Be(3);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/scope-types/{scopeTypeId}/usage-count");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetScopeValueUsageCountAsync_Gets_Usage_Count_Endpoint()
    {
        var scopeValueId = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler().RespondOk(4);
        var service = CreateService(handler);

        var result = await service.GetScopeValueUsageCountAsync(scopeValueId, TestContext.Current.CancellationToken);

        result.Should().Be(4);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/scope-types/values/{scopeValueId}/usage-count");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetScopeSummaryAsync_Gets_Summary_Endpoint()
    {
        var summary = new ScopeSummaryResponse
        {
            ScopeTypes = new List<ScopeTypeDetails>(),
            ScopeValues = new List<ScopeValueDetails>(),
            NodeCount = 1
        };
        var handler = new FakeHttpMessageHandler().RespondOk(summary);
        var service = CreateService(handler);

        var result = await service.GetScopeSummaryAsync(TestContext.Current.CancellationToken);

        result.NodeCount.Should().Be(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/scope-types/summary");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAllScopeTypesWithValuesAsync_Gets_WithValues_Endpoint()
    {
        var payload = new List<ScopeTypeWithValuesDetails>
        {
            new() { ScopeType = new ScopeTypeDetails { Id = Guid.NewGuid(), Name = "Region" }, Values = new List<ScopeValueDetails>() }
        };
        var handler = new FakeHttpMessageHandler().RespondOk(payload);
        var service = CreateService(handler);

        var result = await service.GetAllScopeTypesWithValuesAsync(TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith("api/v1/scope-types/with-values");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetScopeNodesAsync_Gets_Nodes_Endpoint()
    {
        var scopeTypeId = Guid.NewGuid();
        var payload = new List<ScopeNodeInfo> { new() { Id = Guid.NewGuid(), Fqdn = "node1.contoso.com" } };
        var handler = new FakeHttpMessageHandler().RespondOk(payload);
        var service = CreateService(handler);

        var result = await service.GetScopeNodesAsync(scopeTypeId, TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/scope-types/{scopeTypeId}/nodes");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetScopeParametersAsync_Gets_Parameters_Endpoint()
    {
        var schemaId = Guid.NewGuid();
        var scopeTypeId = Guid.NewGuid();
        var payload = new List<ScopeParameterInfo> { new() { ScopeValue = "us-east" } };
        var handler = new FakeHttpMessageHandler().RespondOk(payload);
        var service = CreateService(handler);

        var result = await service.GetScopeParametersAsync(schemaId, scopeTypeId, "us-east", TestContext.Current.CancellationToken);

        result.Should().HaveCount(1);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().Contain($"api/v1/scope-types/parameters/{schemaId}/{scopeTypeId}");
        handler.LastRequest.RequestUri!.ToString().Should().Contain("scopeValue=us-east");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetUnrestrictedScopeValuesAsync_Gets_Unrestricted_Values_Endpoint()
    {
        var scopeTypeId = Guid.NewGuid();
        var payload = new List<string> { "us-east", "us-west" };
        var handler = new FakeHttpMessageHandler().RespondOk(payload);
        var service = CreateService(handler);

        var result = await service.GetUnrestrictedScopeValuesAsync(scopeTypeId, TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.ToString().Should().EndWith($"api/v1/scope-types/{scopeTypeId}/unrestricted-values");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task GetAsync_Returns_KeyNotFoundException_On_404()
    {
        var id = Guid.NewGuid();
        var handler = new FakeHttpMessageHandler()
            .Respond(new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
        var service = CreateService(handler);

        var act = async () => await service.DeleteScopeTypeAsync(id, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
