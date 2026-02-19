// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Server.Contracts;
using OpenDsc.Server.Endpoints;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class NodeTagEndpointsTests : IDisposable
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
        var request = new CreateScopeTypeRequest { Name = name, AllowsValues = true };
        var response = await client.PostAsJsonAsync("/api/v1/scope-types", request);
        var result = await response.Content.ReadFromJsonAsync<ScopeTypeDto>();
        return result!.Id;
    }

    private async Task<Guid> CreateScopeValueAsync(HttpClient client, Guid scopeTypeId, string value)
    {
        var request = new CreateScopeValueRequest { Value = value };
        var response = await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeTypeId}/values", request);
        var result = await response.Content.ReadFromJsonAsync<ScopeValueDto>();
        return result!.Id;
    }

    [Fact]
    public async Task AssignNodeTag_WithValidData_ReturnsCreated()
    {
        using var client = CreateAuthenticatedClient();

        var regKeyRequest = new CreateRegistrationKeyRequest();
        var registrationKeyResponse = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", regKeyRequest);
        registrationKeyResponse.EnsureSuccessStatusCode();
        var regKey = await registrationKeyResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>();
        regKey.Should().NotBeNull();
        string keyValue = regKey!.Key!;

        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", new RegisterNodeRequest { Fqdn = "test.local", RegistrationKey = keyValue });
        var nodeId = (await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>())!.NodeId;

        var scopeTypeId = await CreateScopeTypeAsync(client, "Environment");
        var scopeValueId = await CreateScopeValueAsync(client, scopeTypeId, "Production");
        var request = new AssignNodeTagRequest { ScopeValueId = scopeValueId };

        var response = await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/tags", request);

        response.EnsureSuccessStatusCode();
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var tag = await response.Content.ReadFromJsonAsync<NodeTagDto>();
        tag.Should().NotBeNull();
        tag!.NodeId.Should().Be(nodeId);
        tag.ScopeValueId.Should().Be(scopeValueId);
        tag.ScopeTypeName.Should().Be("Environment");
        tag.ScopeValue.Should().Be("Production");
    }

    [Fact]
    public async Task GetNodeTags_ReturnsList()
    {
        using var client = CreateAuthenticatedClient();

        var regKeyRequest = new CreateRegistrationKeyRequest();
        var registrationKeyResponse = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", regKeyRequest);
        var regKey = await registrationKeyResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>();
        string keyValue = regKey!.Key!;

        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", new RegisterNodeRequest { Fqdn = "test2.local", RegistrationKey = keyValue });
        var nodeId = (await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>())!.NodeId;

        var scopeTypeId = await CreateScopeTypeAsync(client, "Region");
        var scopeValueId = await CreateScopeValueAsync(client, scopeTypeId, "US");
        await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/tags", new AssignNodeTagRequest { ScopeValueId = scopeValueId });

        var response = await client.GetAsync($"/api/v1/nodes/{nodeId}/tags");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<NodeTagDto>>();
        result!.Should().Contain(t => t.ScopeValue == "US");
    }

    [Fact]
    public async Task AssignNodeTag_DuplicateScopeType_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();

        var regKeyRequest = new CreateRegistrationKeyRequest();
        var registrationKeyResponse = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", regKeyRequest);
        var regKey = await registrationKeyResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>();
        string keyValue = regKey!.Key!;

        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", new RegisterNodeRequest { Fqdn = "test3.local", RegistrationKey = keyValue });
        var nodeId = (await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>())!.NodeId;

        var scopeTypeId = await CreateScopeTypeAsync(client, "Environment2");
        var scopeValueId1 = await CreateScopeValueAsync(client, scopeTypeId, "Dev");
        var scopeValueId2 = await CreateScopeValueAsync(client, scopeTypeId, "Prod");

        await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/tags", new AssignNodeTagRequest { ScopeValueId = scopeValueId1 });
        var response = await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/tags", new AssignNodeTagRequest { ScopeValueId = scopeValueId2 });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RemoveNodeTag_WithValidData_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();

        var regKeyRequest = new CreateRegistrationKeyRequest();
        var registrationKeyResponse = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", regKeyRequest);
        var regKey = await registrationKeyResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>();
        string keyValue = regKey!.Key!;

        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", new RegisterNodeRequest { Fqdn = "test4.local", RegistrationKey = keyValue });
        var nodeId = (await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>())!.NodeId;

        var scopeTypeId = await CreateScopeTypeAsync(client, "Tier");
        var scopeValueId = await CreateScopeValueAsync(client, scopeTypeId, "Production");
        await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/tags", new AssignNodeTagRequest { ScopeValueId = scopeValueId });

        var response = await client.DeleteAsync($"/api/v1/nodes/{nodeId}/tags/{scopeValueId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetNodeTags_NonExistentNode_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/nodes/{Guid.NewGuid()}/tags");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignNodeTag_NonExistentNode_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var request = new AssignNodeTagRequest { ScopeValueId = Guid.NewGuid() };

        var response = await client.PostAsJsonAsync($"/api/v1/nodes/{Guid.NewGuid()}/tags", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignNodeTag_NonExistentScopeValue_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var regKeyRequest = new CreateRegistrationKeyRequest();
        var registrationKeyResponse = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", regKeyRequest);
        var regKey = await registrationKeyResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>();
        string keyValue = regKey!.Key!;

        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", new RegisterNodeRequest { Fqdn = "invalidscope.local", RegistrationKey = keyValue });
        var nodeId = (await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>())!.NodeId;

        var request = new AssignNodeTagRequest { ScopeValueId = Guid.NewGuid() };
        var response = await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/tags", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignNodeTag_DuplicateExactTag_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();

        var regKeyRequest = new CreateRegistrationKeyRequest();
        var registrationKeyResponse = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", regKeyRequest);
        var regKey = await registrationKeyResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>();
        string keyValue = regKey!.Key!;

        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", new RegisterNodeRequest { Fqdn = "duplicatetag.local", RegistrationKey = keyValue });
        var nodeId = (await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>())!.NodeId;

        var scopeTypeId = await CreateScopeTypeAsync(client, "DuplicateTest");
        var scopeValueId = await CreateScopeValueAsync(client, scopeTypeId, "Value1");

        await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/tags", new AssignNodeTagRequest { ScopeValueId = scopeValueId });
        var response = await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/tags", new AssignNodeTagRequest { ScopeValueId = scopeValueId });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RemoveNodeTag_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var regKeyRequest = new CreateRegistrationKeyRequest();
        var registrationKeyResponse = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", regKeyRequest);
        var regKey = await registrationKeyResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>();
        string keyValue = regKey!.Key!;

        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", new RegisterNodeRequest { Fqdn = "removetag.local", RegistrationKey = keyValue });
        var nodeId = (await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>())!.NodeId;

        var response = await client.DeleteAsync($"/api/v1/nodes/{nodeId}/tags/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
