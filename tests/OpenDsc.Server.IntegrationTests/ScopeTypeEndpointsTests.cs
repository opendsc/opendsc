// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Server.Endpoints;
using OpenDsc.Server.Entities;

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

    [Fact]
    public async Task GetAllScopeTypes_ReturnsSystemScopes()
    {
        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync("/api/v1/scope-types");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ScopeTypeDto>>(SourceGenerationContext.Default.Options);
        result!.Should().Contain(st => st.Name == "Default");
        result.Should().Contain(st => st.Name == "Node");
    }

    [Fact]
    public async Task CreateScopeType_WithValidData_ReturnsCreated()
    {
        using var client = CreateAuthenticatedClient();
        var request = new CreateScopeTypeRequest { Name = "Environment", ValueMode = ScopeValueMode.Restricted };

        var response = await client.PostAsJsonAsync("/api/v1/scope-types", request, SourceGenerationContext.Default.Options);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options);
        result!.Name.Should().Be("Environment");
    }

    [Fact]
    public async Task CreateScopeType_DuplicateName_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();
        var request = new CreateScopeTypeRequest { Name = "TestScope1", ValueMode = ScopeValueMode.Restricted };

        await client.PostAsJsonAsync("/api/v1/scope-types", request);
        var response = await client.PostAsJsonAsync("/api/v1/scope-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateScopeType_WithValidData_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();
        var createRequest = new CreateScopeTypeRequest { Name = "TestScope2", ValueMode = ScopeValueMode.Restricted };
        var createResponse = await client.PostAsJsonAsync("/api/v1/scope-types", createRequest, SourceGenerationContext.Default.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options);

        var updateRequest = new UpdateScopeTypeRequest { Description = "Updated description" };
        var response = await client.PutAsJsonAsync($"/api/v1/scope-types/{created!.Id}", updateRequest, SourceGenerationContext.Default.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options);
        result!.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task DeleteScopeType_NonSystemScope_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();
        var createRequest = new CreateScopeTypeRequest { Name = "TestScope3", ValueMode = ScopeValueMode.Restricted };
        var createResponse = await client.PostAsJsonAsync("/api/v1/scope-types", createRequest, SourceGenerationContext.Default.Options);
        var created = await createResponse.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options);

        var response = await client.DeleteAsync($"/api/v1/scope-types/{created!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteScopeType_SystemScope_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();
        var allScopes = await client.GetFromJsonAsync<List<ScopeTypeDto>>("/api/v1/scope-types", SourceGenerationContext.Default.Options);
        var defaultScope = allScopes!.First(s => s.Name == "Default");

        var response = await client.DeleteAsync($"/api/v1/scope-types/{defaultScope.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ReorderScopeTypes_WithValidData_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();

        await client.PostAsJsonAsync("/api/v1/scope-types", new CreateScopeTypeRequest { Name = "Custom1", ValueMode = ScopeValueMode.Restricted }, SourceGenerationContext.Default.Options);
        await client.PostAsJsonAsync("/api/v1/scope-types", new CreateScopeTypeRequest { Name = "Custom2", ValueMode = ScopeValueMode.Restricted }, SourceGenerationContext.Default.Options);

        var allScopes = await client.GetFromJsonAsync<List<ScopeTypeDto>>("/api/v1/scope-types", SourceGenerationContext.Default.Options);

        var defaultScope = allScopes!.First(s => s.Name == "Default");
        var nodeScope = allScopes!.First(s => s.Name == "Node");
        var custom1 = allScopes!.First(s => s.Name == "Custom1");
        var custom2 = allScopes!.First(s => s.Name == "Custom2");

        var orderedIds = new List<Guid> { defaultScope.Id, custom2.Id, custom1.Id, nodeScope.Id };
        var request = new ReorderScopeTypesRequest { ScopeTypeIds = orderedIds };

        var response = await client.PutAsJsonAsync("/api/v1/scope-types/reorder", request, SourceGenerationContext.Default.Options);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ScopeTypeDto>>(SourceGenerationContext.Default.Options);
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
        var allScopes = await client.GetFromJsonAsync<List<ScopeTypeDto>>("/api/v1/scope-types", SourceGenerationContext.Default.Options);
        var defaultScope = allScopes!.First(s => s.Name == "Default");

        var response = await client.GetAsync($"/api/v1/scope-types/{defaultScope.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options);
        result!.Name.Should().Be("Default");
    }

    [Fact]
    public async Task GetScopeType_WithInvalidId_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var response = await client.GetAsync($"/api/v1/scope-types/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateScopeType_WithEmptyName_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var request = new CreateScopeTypeRequest { Name = "", ValueMode = ScopeValueMode.Restricted };

        var response = await client.PostAsJsonAsync("/api/v1/scope-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateScopeType_WithInvalidCharacters_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var request = new CreateScopeTypeRequest { Name = "Invalid Name!", ValueMode = ScopeValueMode.Restricted };

        var response = await client.PostAsJsonAsync("/api/v1/scope-types", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateScopeType_WithNonExistentId_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var updateRequest = new UpdateScopeTypeRequest { Description = "Test" };

        var response = await client.PutAsJsonAsync($"/api/v1/scope-types/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateScopeType_SystemScope_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var allScopes = await client.GetFromJsonAsync<List<ScopeTypeDto>>("/api/v1/scope-types", SourceGenerationContext.Default.Options);
        var defaultScope = allScopes!.First(s => s.Name == "Default");

        var updateRequest = new UpdateScopeTypeRequest { Description = "Cannot update" };
        var response = await client.PutAsJsonAsync($"/api/v1/scope-types/{defaultScope.Id}", updateRequest, SourceGenerationContext.Default.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteScopeType_WithScopeValues_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();
        var createRequest = new CreateScopeTypeRequest { Name = "WithValues", ValueMode = ScopeValueMode.Restricted };
        var createResponse = await client.PostAsJsonAsync("/api/v1/scope-types", createRequest, SourceGenerationContext.Default.Options);
        var scopeType = await createResponse.Content.ReadFromJsonAsync<ScopeTypeDto>(SourceGenerationContext.Default.Options);

        await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeType!.Id}/values", new CreateScopeValueRequest { Value = "TestValue" }, SourceGenerationContext.Default.Options);

        var response = await client.DeleteAsync($"/api/v1/scope-types/{scopeType.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteScopeType_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();
        var response = await client.DeleteAsync($"/api/v1/scope-types/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReorderScopeTypes_EmptyArray_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var request = new ReorderScopeTypesRequest { ScopeTypeIds = new List<Guid>() };

        var response = await client.PutAsJsonAsync("/api/v1/scope-types/reorder", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderScopeTypes_DefaultNotFirst_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var allScopes = await client.GetFromJsonAsync<List<ScopeTypeDto>>("/api/v1/scope-types", SourceGenerationContext.Default.Options);
        var defaultScope = allScopes!.First(s => s.Name == "Default");
        var nodeScope = allScopes!.First(s => s.Name == "Node");

        var orderedIds = new List<Guid> { nodeScope.Id, defaultScope.Id };
        var request = new ReorderScopeTypesRequest { ScopeTypeIds = orderedIds };

        var response = await client.PutAsJsonAsync("/api/v1/scope-types/reorder", request, SourceGenerationContext.Default.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderScopeTypes_NodeNotLast_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        await client.PostAsJsonAsync("/api/v1/scope-types", new CreateScopeTypeRequest { Name = "Custom3", ValueMode = ScopeValueMode.Restricted }, SourceGenerationContext.Default.Options);

        var allScopes = await client.GetFromJsonAsync<List<ScopeTypeDto>>("/api/v1/scope-types", SourceGenerationContext.Default.Options);
        var defaultScope = allScopes!.First(s => s.Name == "Default");
        var nodeScope = allScopes!.First(s => s.Name == "Node");
        var custom3 = allScopes!.First(s => s.Name == "Custom3");

        var orderedIds = new List<Guid> { defaultScope.Id, nodeScope.Id, custom3.Id };
        var request = new ReorderScopeTypesRequest { ScopeTypeIds = orderedIds };

        var response = await client.PutAsJsonAsync("/api/v1/scope-types/reorder", request, SourceGenerationContext.Default.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReorderScopeTypes_InvalidIds_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var request = new ReorderScopeTypesRequest { ScopeTypeIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() } };

        var response = await client.PutAsJsonAsync("/api/v1/scope-types/reorder", request, SourceGenerationContext.Default.Options);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
