// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Endpoints;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class ScopeValueEndpointsTests : IDisposable
{
    private readonly ServerWebApplicationFactory _factory = new();

    public void Dispose()
    {
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        return _factory.CreateAuthenticatedClient();
    }

    private async Task<Guid> CreateScopeTypeAsync(HttpClient client, string name)
    {
        var request = new CreateScopeTypeRequest { Name = name, ValueMode = ScopeValueMode.Restricted };
        var response = await client.PostAsJsonAsync("/api/v1/scope-types", request, SourceGenerationContext.Default.Options);
        var result = await response.Content.ReadFromJsonAsync<ScopeTypeDetails>(SourceGenerationContext.Default.Options);
        return result!.Id;
    }

    [Fact]
    public async Task CreateScopeValue_WithValidData_ReturnsCreated()
    {
        using var client = CreateAuthenticatedClient();
        var scopeTypeId = await CreateScopeTypeAsync(client, "Environment");
        var request = new CreateScopeValueRequest { Value = "Production" };

        var response = await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeTypeId}/values", request, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ScopeValueDetails>(SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        result!.Value.Should().Be("Production");
    }

    [Fact]
    public async Task GetScopeValues_ReturnsList()
    {
        using var client = CreateAuthenticatedClient();
        var scopeTypeId = await CreateScopeTypeAsync(client, "Region");
        await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeTypeId}/values", new CreateScopeValueRequest { Value = "US" }, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        var response = await client.GetAsync($"/api/v1/scope-types/{scopeTypeId}/values", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ScopeValueDetails>>(SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        result!.Should().Contain(sv => sv.Value == "US");
    }

    [Fact]
    public async Task CreateScopeValue_DuplicateValue_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();
        var scopeTypeId = await CreateScopeTypeAsync(client, "Location");
        var request = new CreateScopeValueRequest { Value = "TestValue1" };

        await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeTypeId}/values", request, TestContext.Current.CancellationToken);
        var response = await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeTypeId}/values", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateScopeValue_ScopeTypeDoesNotAllowValues_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var createScopeType = new CreateScopeTypeRequest { Name = "NoValuesScope", ValueMode = ScopeValueMode.Unrestricted };
        var scopeTypeResponse = await client.PostAsJsonAsync("/api/v1/scope-types", createScopeType, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        var scopeType = await scopeTypeResponse.Content.ReadFromJsonAsync<ScopeTypeDetails>(SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        var request = new CreateScopeValueRequest { Value = "InvalidValue" };
        var response = await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeType!.Id}/values", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateScopeValue_WithValidData_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();
        var scopeTypeId = await CreateScopeTypeAsync(client, "Datacenter");
        var valueId = await CreateScopeValueAsync(client, scopeTypeId, "DC1");

        var updateRequest = new UpdateScopeValueRequest { Description = "Primary datacenter" };
        var response = await client.PutAsJsonAsync($"/api/v1/scope-types/{scopeTypeId}/values/{valueId}", updateRequest, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ScopeValueDetails>(SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        result!.Description.Should().Be("Primary datacenter");
    }

    [Fact]
    public async Task DeleteScopeValue_WithValidId_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();
        var scopeTypeId = await CreateScopeTypeAsync(client, "Zone");
        var valueId = await CreateScopeValueAsync(client, scopeTypeId, "Zone1");

        var response = await client.DeleteAsync($"/api/v1/scope-types/{scopeTypeId}/values/{valueId}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task<Guid> CreateScopeValueAsync(HttpClient client, Guid scopeTypeId, string value)
    {
        var request = new CreateScopeValueRequest { Value = value };
        var response = await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeTypeId}/values", request);
        var result = await response.Content.ReadFromJsonAsync<ScopeValueDetails>();
        return result!.Id;
    }

    [Fact]
    public async Task GetScopeValue_WithValidId_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();
        var scopeTypeId = await CreateScopeTypeAsync(client, "GetTest");
        var valueId = await CreateScopeValueAsync(client, scopeTypeId, "TestValue");

        var response = await client.GetAsync($"/api/v1/scope-types/{scopeTypeId}/values/{valueId}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ScopeValueDetails>(TestContext.Current.CancellationToken);
        result!.Value.Should().Be("TestValue");
    }

    [Fact]
    public async Task GetScopeValue_WithInvalidId_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var scopeTypeId = await CreateScopeTypeAsync(client, "NotFoundTest");

        var response = await client.GetAsync($"/api/v1/scope-types/{scopeTypeId}/values/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetScopeValues_NonExistentScopeType_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/scope-types/{Guid.NewGuid()}/values", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateScopeValue_EmptyValue_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var scopeTypeId = await CreateScopeTypeAsync(client, "EmptyTest");
        var request = new CreateScopeValueRequest { Value = "" };

        var response = await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeTypeId}/values", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateScopeValue_InvalidCharacters_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var scopeTypeId = await CreateScopeTypeAsync(client, "InvalidCharsTest");
        var request = new CreateScopeValueRequest { Value = "Invalid Value!" };

        var response = await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeTypeId}/values", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateScopeValue_NonExistentScopeType_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var request = new CreateScopeValueRequest { Value = "Test" };

        var response = await client.PostAsJsonAsync($"/api/v1/scope-types/{Guid.NewGuid()}/values", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateScopeValue_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var scopeTypeId = await CreateScopeTypeAsync(client, "UpdateNotFoundTest");
        var updateRequest = new UpdateScopeValueRequest { Description = "Test" };

        var response = await client.PutAsJsonAsync($"/api/v1/scope-types/{scopeTypeId}/values/{Guid.NewGuid()}", updateRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteScopeValue_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var scopeTypeId = await CreateScopeTypeAsync(client, "DeleteNotFoundTest");

        var response = await client.DeleteAsync($"/api/v1/scope-types/{scopeTypeId}/values/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteScopeValue_WithNodeTags_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();

        var regKeyRequest = new CreateRegistrationKeyRequest();
        var registrationKeyResponse = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", regKeyRequest, TestContext.Current.CancellationToken);
        var regKey = await registrationKeyResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>(TestContext.Current.CancellationToken);
        string keyValue = regKey!.Key!;

        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", new RegisterNodeRequest { Fqdn = "deletetest.local", RegistrationKey = keyValue }, TestContext.Current.CancellationToken);
        var nodeId = (await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>(TestContext.Current.CancellationToken))!.NodeId;

        var scopeTypeId = await CreateScopeTypeAsync(client, "DeleteWithTagsTest");
        var scopeValueId = await CreateScopeValueAsync(client, scopeTypeId, "TaggedValue");

        await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/tags", new AddNodeTagRequest { ScopeValueId = scopeValueId }, TestContext.Current.CancellationToken);

        var response = await client.DeleteAsync($"/api/v1/scope-types/{scopeTypeId}/values/{scopeValueId}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}

