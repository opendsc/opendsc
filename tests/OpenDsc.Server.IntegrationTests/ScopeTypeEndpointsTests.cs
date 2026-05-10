// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Data;
using OpenDsc.Server.Endpoints;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class ScopeTypeEndpointsTests : IDisposable
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
        var result = await response.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options);
        return result!.Id;
    }

    private async Task<Guid> CreateScopeValueAsync(HttpClient client, Guid scopeTypeId, string value)
    {
        var request = new CreateScopeValueRequest { Value = value };
        var response = await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeTypeId}/values", request);
        var result = await response.Content.ReadFromJsonAsync<ScopeValueDto>();
        return result!.Id;
    }

    private async Task<Guid> CreateTestConfigurationAsync(HttpClient client, string name)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "name");
        content.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var file = new ByteArrayContent("resources: []"u8.ToArray());
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(file, "files", "main.dsc.yaml");

        var response = await client.PostAsync("/api/v1/configurations", content);
        response.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
        var config = await db.Configurations.FirstOrDefaultAsync(c => c.Name == name);
        if (config is null)
        {
            throw new InvalidOperationException($"Configuration '{name}' was not found after creation");
        }
        return config.Id;
    }

    [Fact]
    public async Task GetAllScopeTypes_ReturnsSystemScopes()
    {
        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/scope-types", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ScopeTypeDto>>(SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        result!.Should().Contain(st => st.Name == "Default");
        result.Should().Contain(st => st.Name == "Node");
    }

    [Fact]
    public async Task CreateScopeType_WithValidData_ReturnsCreated()
    {
        using var client = CreateAuthenticatedClient();
        var request = new CreateScopeTypeRequest { Name = "Environment", ValueMode = ScopeValueMode.Restricted };

        var response = await client.PostAsJsonAsync("/api/v1/scope-types", request, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        result!.Name.Should().Be("Environment");
    }

    [Fact]
    public async Task CreateScopeType_DuplicateName_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();
        var request = new CreateScopeTypeRequest { Name = "TestScope1", ValueMode = ScopeValueMode.Restricted };

        await client.PostAsJsonAsync("/api/v1/scope-types", request, TestContext.Current.CancellationToken);
        var response = await client.PostAsJsonAsync("/api/v1/scope-types", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateScopeType_WithValidData_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();
        var createRequest = new CreateScopeTypeRequest { Name = "TestScope2", ValueMode = ScopeValueMode.Restricted };
        var createResponse = await client.PostAsJsonAsync("/api/v1/scope-types", createRequest, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        var updateRequest = new UpdateScopeTypeRequest { Description = "Updated description" };
        var response = await client.PutAsJsonAsync($"/api/v1/scope-types/{created!.Id}", updateRequest, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        result!.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteScopeType_NonSystemScope_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();
        var createRequest = new CreateScopeTypeRequest { Name = "TestScope3", ValueMode = ScopeValueMode.Restricted };
        var createResponse = await client.PostAsJsonAsync("/api/v1/scope-types", createRequest, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        var created = await createResponse.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        var response = await client.DeleteAsync($"/api/v1/scope-types/{created!.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteScopeType_SystemScope_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();
        var allScopes = await client.GetFromJsonAsync<List<ScopeTypeDto>>("/api/v1/scope-types", SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        var defaultScope = allScopes!.First(s => s.Name == "Default");

        var response = await client.DeleteAsync($"/api/v1/scope-types/{defaultScope.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReorderScopeTypes_WithValidData_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();

        await client.PostAsJsonAsync("/api/v1/scope-types", new CreateScopeTypeRequest { Name = "Custom1", ValueMode = ScopeValueMode.Restricted }, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        await client.PostAsJsonAsync("/api/v1/scope-types", new CreateScopeTypeRequest { Name = "Custom2", ValueMode = ScopeValueMode.Restricted }, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        var allScopes = await client.GetFromJsonAsync<List<ScopeTypeDto>>("/api/v1/scope-types", SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        var defaultScope = allScopes!.First(s => s.Name == "Default");
        var nodeScope = allScopes!.First(s => s.Name == "Node");
        var custom1 = allScopes!.First(s => s.Name == "Custom1");
        var custom2 = allScopes!.First(s => s.Name == "Custom2");

        var orderedIds = new List<Guid> { defaultScope.Id, custom2.Id, custom1.Id, nodeScope.Id };
        var request = new ReorderScopeTypesRequest { ScopeTypeIds = orderedIds };

        var response = await client.PutAsJsonAsync("/api/v1/scope-types/reorder", request, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ScopeTypeDto>>(SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        result!.Count.Should().Be(4);
        result[0].Name.Should().Be("Default");
        result[1].Name.Should().Be("Custom2");
        result[2].Name.Should().Be("Custom1");
        result[3].Name.Should().Be("Node");
    }

    [Fact]
    public async Task GetScopeType_WithValidId_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();
        var allScopes = await client.GetFromJsonAsync<List<ScopeTypeDto>>("/api/v1/scope-types", SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        var defaultScope = allScopes!.First(s => s.Name == "Default");

        var response = await client.GetAsync($"/api/v1/scope-types/{defaultScope.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        result!.Name.Should().Be("Default");
    }

    [Fact]
    public async Task GetScopeType_WithInvalidId_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/scope-types/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateScopeType_WithEmptyName_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var request = new CreateScopeTypeRequest { Name = "", ValueMode = ScopeValueMode.Restricted };

        var response = await client.PostAsJsonAsync("/api/v1/scope-types", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateScopeType_WithInvalidCharacters_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var request = new CreateScopeTypeRequest { Name = "Invalid Name!", ValueMode = ScopeValueMode.Restricted };

        var response = await client.PostAsJsonAsync("/api/v1/scope-types", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateScopeType_WithNonExistentId_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var updateRequest = new UpdateScopeTypeRequest { Description = "Test" };

        var response = await client.PutAsJsonAsync($"/api/v1/scope-types/{Guid.NewGuid()}", updateRequest, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateScopeType_SystemScope_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var allScopes = await client.GetFromJsonAsync<List<ScopeTypeDto>>("/api/v1/scope-types", SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        var defaultScope = allScopes!.First(s => s.Name == "Default");

        var updateRequest = new UpdateScopeTypeRequest { Description = "Cannot update" };
        var response = await client.PutAsJsonAsync($"/api/v1/scope-types/{defaultScope.Id}", updateRequest, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // after adding values alone we should still be able to delete until those values are used
    [Fact]
    public async Task DeleteScopeType_WithScopeValuesButUnused_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();
        var createRequest = new CreateScopeTypeRequest { Name = "WithValues", ValueMode = ScopeValueMode.Restricted };
        var createResponse = await client.PostAsJsonAsync("/api/v1/scope-types", createRequest, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        var scopeType = await createResponse.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeType!.Id}/values", new CreateScopeValueRequest { Value = "TestValue" }, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        var response = await client.DeleteAsync($"/api/v1/scope-types/{scopeType.Id}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteScopeType_WithValueUsedByNode_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();
        var createRequest = new CreateScopeTypeRequest { Name = "WithNode", ValueMode = ScopeValueMode.Restricted };
        var createResponse = await client.PostAsJsonAsync("/api/v1/scope-types", createRequest, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        var scopeType = await createResponse.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        var valueResponse = await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeType!.Id}/values", new CreateScopeValueRequest { Value = "Used" }, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        var scopeValue = await valueResponse.Content.ReadFromJsonAsync<ScopeValueDto>(TestContext.Current.CancellationToken);

        // assign a node tag so the value becomes "used"
        var regKeyRequest = new CreateRegistrationKeyRequest();
        var registrationKeyResponse = await client.PostAsJsonAsync("/api/v1/admin/registration-keys", regKeyRequest, TestContext.Current.CancellationToken);
        var regKey = await registrationKeyResponse.Content.ReadFromJsonAsync<RegistrationKeyResponse>(TestContext.Current.CancellationToken);
        string keyValue = regKey!.Key!;

        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", new RegisterNodeRequest { Fqdn = "delete-node.local", RegistrationKey = keyValue }, TestContext.Current.CancellationToken);
        var nodeId = (await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>(TestContext.Current.CancellationToken))!.NodeId;
        await client.PostAsJsonAsync($"/api/v1/nodes/{nodeId}/tags", new AddNodeTagRequest { ScopeValueId = scopeValue!.Id }, TestContext.Current.CancellationToken);

        var response = await client.DeleteAsync($"/api/v1/scope-types/{scopeType.Id}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteScopeType_WithParameterFiles_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();
        var scopeTypeId = await CreateScopeTypeAsync(client, "PFType");
        var scopeValueId = await CreateScopeValueAsync(client, scopeTypeId, "PFValue");

        // create a configuration and add a parameter file scoped to the value
        var configId = await CreateTestConfigurationAsync(client, $"config-{Guid.NewGuid()}");
        var request = new
        {
            scopeValue = "PFValue",
            version = "1.0.0",
            content = "param: value",
            contentType = "application/x-yaml",
            isDraft = false
        };
        var response1 = await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", request, TestContext.Current.CancellationToken);
        response1.EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/v1/scope-types/{scopeTypeId}", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteScopeType_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"/api/v1/scope-types/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReorderScopeTypes_EmptyArray_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var request = new ReorderScopeTypesRequest { ScopeTypeIds = new List<Guid>() };

        var response = await client.PutAsJsonAsync("/api/v1/scope-types/reorder", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderScopeTypes_DefaultNotFirst_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var allScopes = await client.GetFromJsonAsync<List<ScopeTypeDto>>("/api/v1/scope-types", SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        var defaultScope = allScopes!.First(s => s.Name == "Default");
        var nodeScope = allScopes!.First(s => s.Name == "Node");

        var orderedIds = new List<Guid> { nodeScope.Id, defaultScope.Id };
        var request = new ReorderScopeTypesRequest { ScopeTypeIds = orderedIds };

        var response = await client.PutAsJsonAsync("/api/v1/scope-types/reorder", request, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderScopeTypes_NodeNotLast_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        await client.PostAsJsonAsync("/api/v1/scope-types", new CreateScopeTypeRequest { Name = "Custom3", ValueMode = ScopeValueMode.Restricted }, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        var allScopes = await client.GetFromJsonAsync<List<ScopeTypeDto>>("/api/v1/scope-types", SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);
        var defaultScope = allScopes!.First(s => s.Name == "Default");
        var nodeScope = allScopes!.First(s => s.Name == "Node");
        var custom3 = allScopes!.First(s => s.Name == "Custom3");

        var orderedIds = new List<Guid> { defaultScope.Id, nodeScope.Id, custom3.Id };
        var request = new ReorderScopeTypesRequest { ScopeTypeIds = orderedIds };

        var response = await client.PutAsJsonAsync("/api/v1/scope-types/reorder", request, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderScopeTypes_InvalidIds_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var request = new ReorderScopeTypesRequest { ScopeTypeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() } };

        var response = await client.PutAsJsonAsync("/api/v1/scope-types/reorder", request, SourceGenerationContext.Default.Options, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
