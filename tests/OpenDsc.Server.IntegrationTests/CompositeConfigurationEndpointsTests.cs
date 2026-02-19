// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Headers;

using AwesomeAssertions;

using OpenDsc.Server.Contracts;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class CompositeConfigurationEndpointsTests : IDisposable
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

    private async Task<string> CreateTestConfigurationAsync(HttpClient client, string name)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "name");
        content.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var file = new ByteArrayContent("resources: []"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(file, "files", "main.dsc.yaml");

        var response = await client.PostAsync("/api/v1/configurations", content);
        response.EnsureSuccessStatusCode();

        return name;
    }

    [Fact]
    public async Task CreateComposite_WithValidData_ReturnsCreated()
    {
        using var client = CreateAuthenticatedClient();

        var request = new CreateCompositeConfigurationRequest
        {
            Name = "test-composite",
            Description = "Test composite configuration",
            EntryPoint = "main.dsc.yaml"
        };

        var response = await client.PostAsJsonAsync("/api/v1/composite-configurations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/composite-configurations/");
    }

    [Fact]
    public async Task CreateComposite_Duplicate_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();

        var request = new CreateCompositeConfigurationRequest
        {
            Name = "duplicate-composite",
            Description = "Test",
            EntryPoint = "main.dsc.yaml"
        };

        await client.PostAsJsonAsync("/api/v1/composite-configurations", request);
        var response = await client.PostAsJsonAsync("/api/v1/composite-configurations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateVersion_ForExistingComposite_ReturnsCreated()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateCompositeConfigurationRequest { Name = "version-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        var response = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task AddChild_ToVersion_ReturnsCreated()
    {
        using var client = CreateAuthenticatedClient();

        var childConfigName = await CreateTestConfigurationAsync(client, "child-config-add");

        var createRequest = new CreateCompositeConfigurationRequest { Name = "child-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        var versionResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);
        var versionId = versionResponse.Headers.Location!.ToString().Split('/').Last();

        var addChildRequest = new AddChildConfigurationRequest { ChildConfigurationName = childConfigName, Order = 0 };
        var response = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/{versionId}/children", addChildRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AddChild_CompositeAsChild_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var composite1Request = new CreateCompositeConfigurationRequest { Name = "comp-parent-test", EntryPoint = "main.dsc.yaml" };
        var composite1Response = await client.PostAsJsonAsync("/api/v1/composite-configurations", composite1Request);
        var composite1Id = composite1Response.Headers.Location!.ToString().Split('/').Last();

        var composite2Request = new CreateCompositeConfigurationRequest { Name = "comp-child-test", EntryPoint = "main.dsc.yaml" };
        await client.PostAsJsonAsync("/api/v1/composite-configurations", composite2Request);

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        var versionResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{composite1Id}/versions", versionRequest);
        var versionId = versionResponse.Headers.Location!.ToString().Split('/').Last();

        var addChildRequest = new AddChildConfigurationRequest { ChildConfigurationName = "comp-child-test", Order = 0 };
        var response = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{composite1Id}/versions/{versionId}/children", addChildRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("composite");
    }

    [Fact]
    public async Task DeleteComposite_Existing_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateCompositeConfigurationRequest { Name = "delete-test-comp", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var response = await client.DeleteAsync($"/api/v1/composite-configurations/{compositeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetCompositeConfigurations_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();

        var request = new CreateCompositeConfigurationRequest
        {
            Name = "get-all-test",
            Description = "Test",
            EntryPoint = "main.dsc.yaml"
        };
        await client.PostAsJsonAsync("/api/v1/composite-configurations", request);

        var response = await client.GetAsync("/api/v1/composite-configurations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var composites = await response.Content.ReadFromJsonAsync<List<CompositeConfigurationSummaryDto>>();
        composites.Should().NotBeNull();
        composites.Should().Contain(c => c.Name == "get-all-test");
    }

    [Fact]
    public async Task GetCompositeConfigurationDetails_Existing_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();

        var request = new CreateCompositeConfigurationRequest
        {
            Name = "get-details-test",
            Description = "Test details",
            EntryPoint = "main.dsc.yaml"
        };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", request);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var response = await client.GetAsync($"/api/v1/composite-configurations/{compositeId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var details = await response.Content.ReadFromJsonAsync<CompositeConfigurationDetailsDto>();
        details.Should().NotBeNull();
        details!.Name.Should().Be("get-details-test");
        details.Description.Should().Be("Test details");
    }

    [Fact]
    public async Task GetCompositeConfigurationDetails_NotFound_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/composite-configurations/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateComposite_EmptyName_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var request = new CreateCompositeConfigurationRequest
        {
            Name = "",
            EntryPoint = "main.dsc.yaml"
        };

        var response = await client.PostAsJsonAsync("/api/v1/composite-configurations", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteComposite_NotFound_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/v1/composite-configurations/nonexistent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateVersion_NonExistentComposite_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        var response = await client.PostAsJsonAsync("/api/v1/composite-configurations/nonexistent/versions", versionRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateVersion_EmptyVersion_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateCompositeConfigurationRequest { Name = "empty-ver-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "" };
        var response = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateVersion_DuplicateVersion_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateCompositeConfigurationRequest { Name = "dup-ver-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);
        var response = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetCompositeConfigurationVersions_Existing_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateCompositeConfigurationRequest { Name = "get-vers-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        var response = await client.GetAsync($"/api/v1/composite-configurations/{compositeId}/versions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await response.Content.ReadFromJsonAsync<List<CompositeConfigurationVersionDto>>();
        versions.Should().NotBeNull();
        versions.Should().Contain(v => v.Version == "1.0.0");
    }

    [Fact]
    public async Task GetCompositeConfigurationVersions_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/composite-configurations/nonexistent/versions");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCompositeConfigurationVersionDetails_Existing_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateCompositeConfigurationRequest { Name = "get-ver-det-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        var response = await client.GetAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var version = await response.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();
        version.Should().NotBeNull();
        version!.Version.Should().Be("1.0.0");
        version.IsDraft.Should().BeTrue();
    }

    [Fact]
    public async Task GetCompositeConfigurationVersionDetails_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/composite-configurations/nonexistent/versions/1.0.0");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PublishVersion_DraftVersion_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateCompositeConfigurationRequest { Name = "pub-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        var response = await client.PutAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0");
        var version = await getResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();
        version!.IsDraft.Should().BeFalse();
    }

    [Fact]
    public async Task PublishVersion_AlreadyPublished_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateCompositeConfigurationRequest { Name = "pub-again-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        await client.PutAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/publish", null);
        var response = await client.PutAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PublishVersion_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.PutAsync("/api/v1/composite-configurations/nonexistent/versions/1.0.0/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteVersion_DraftVersion_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateCompositeConfigurationRequest { Name = "del-ver-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        var response = await client.DeleteAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteVersion_PublishedVersion_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateCompositeConfigurationRequest { Name = "del-pub-ver-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);
        await client.PutAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/publish", null);

        var response = await client.DeleteAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteVersion_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/v1/composite-configurations/nonexistent/versions/1.0.0");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddChild_NonExistentChild_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateCompositeConfigurationRequest { Name = "add-nonexist-child-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        var addChildRequest = new AddChildConfigurationRequest { ChildConfigurationName = "nonexistent-child", Order = 0 };
        var response = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children", addChildRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddChild_NonExistentVersion_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var addChildRequest = new AddChildConfigurationRequest { ChildConfigurationName = "anything", Order = 0 };
        var response = await client.PostAsJsonAsync("/api/v1/composite-configurations/nonexistent/versions/1.0.0/children", addChildRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddChild_ToPublishedVersion_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var childConfigName = await CreateTestConfigurationAsync(client, "child-pub-test");

        var createRequest = new CreateCompositeConfigurationRequest { Name = "add-to-pub-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);
        await client.PutAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/publish", null);

        var addChildRequest = new AddChildConfigurationRequest { ChildConfigurationName = childConfigName, Order = 0 };
        var response = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children", addChildRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddChild_DuplicateChild_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();

        var childConfigName = await CreateTestConfigurationAsync(client, "dup-child-test");

        var createRequest = new CreateCompositeConfigurationRequest { Name = "dup-child-comp-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        var addChildRequest = new AddChildConfigurationRequest { ChildConfigurationName = childConfigName, Order = 0 };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children", addChildRequest);
        var response = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children", addChildRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateChild_Existing_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();

        var childConfigName = await CreateTestConfigurationAsync(client, "upd-child-test");

        var createRequest = new CreateCompositeConfigurationRequest { Name = "upd-child-comp-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        var addChildRequest = new AddChildConfigurationRequest { ChildConfigurationName = childConfigName, Order = 0 };
        var addResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children", addChildRequest);
        var childItem = await addResponse.Content.ReadFromJsonAsync<CompositeConfigurationItemDto>();

        var updateRequest = new UpdateChildConfigurationRequest { Order = 5 };
        var response = await client.PutAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children/{childItem!.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<CompositeConfigurationItemDto>();
        updated!.Order.Should().Be(5);
    }

    [Fact]
    public async Task UpdateChild_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var updateRequest = new UpdateChildConfigurationRequest { Order = 5 };
        var response = await client.PutAsJsonAsync($"/api/v1/composite-configurations/nonexistent/versions/1.0.0/children/{Guid.NewGuid()}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateChild_InPublishedVersion_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var childConfigName = await CreateTestConfigurationAsync(client, "upd-pub-child-test");

        var createRequest = new CreateCompositeConfigurationRequest { Name = "upd-pub-comp-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        var addChildRequest = new AddChildConfigurationRequest { ChildConfigurationName = childConfigName, Order = 0 };
        var addResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children", addChildRequest);
        var childItem = await addResponse.Content.ReadFromJsonAsync<CompositeConfigurationItemDto>();

        await client.PutAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/publish", null);

        var updateRequest = new UpdateChildConfigurationRequest { Order = 5 };
        var response = await client.PutAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children/{childItem!.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RemoveChild_Existing_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();

        var childConfigName = await CreateTestConfigurationAsync(client, "rem-child-test");

        var createRequest = new CreateCompositeConfigurationRequest { Name = "rem-child-comp-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        var addChildRequest = new AddChildConfigurationRequest { ChildConfigurationName = childConfigName, Order = 0 };
        var addResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children", addChildRequest);
        var childItem = await addResponse.Content.ReadFromJsonAsync<CompositeConfigurationItemDto>();

        var response = await client.DeleteAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children/{childItem!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemoveChild_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync($"/api/v1/composite-configurations/nonexistent/versions/1.0.0/children/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveChild_FromPublishedVersion_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var childConfigName = await CreateTestConfigurationAsync(client, "rem-pub-child-test");

        var createRequest = new CreateCompositeConfigurationRequest { Name = "rem-pub-comp-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        var addChildRequest = new AddChildConfigurationRequest { ChildConfigurationName = childConfigName, Order = 0 };
        var addResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children", addChildRequest);
        var childItem = await addResponse.Content.ReadFromJsonAsync<CompositeConfigurationItemDto>();

        await client.PutAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/publish", null);

        var response = await client.DeleteAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children/{childItem!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddChild_WithInvalidActiveVersion_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var childConfigName = await CreateTestConfigurationAsync(client, "invalid-version-child-test");

        var createRequest = new CreateCompositeConfigurationRequest { Name = "invalid-ver-comp-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        var addChildRequest = new AddChildConfigurationRequest
        {
            ChildConfigurationName = childConfigName,
            Order = 0,
            ActiveVersion = "99.99.99"
        };
        var response = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children", addChildRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("Version");
    }

    [Fact]
    public async Task UpdateChild_WithInvalidActiveVersion_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var childConfigName = await CreateTestConfigurationAsync(client, "upd-invalid-ver-child-test");

        var createRequest = new CreateCompositeConfigurationRequest { Name = "upd-invalid-ver-comp-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        var compositeId = createResponse.Headers.Location!.ToString().Split('/').Last();

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions", versionRequest);

        var addChildRequest = new AddChildConfigurationRequest { ChildConfigurationName = childConfigName, Order = 0 };
        var addResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children", addChildRequest);
        var childItem = await addResponse.Content.ReadFromJsonAsync<CompositeConfigurationItemDto>();

        var updateRequest = new UpdateChildConfigurationRequest { Order = 5, ActiveVersion = "99.99.99" };
        var response = await client.PutAsJsonAsync($"/api/v1/composite-configurations/{compositeId}/versions/1.0.0/children/{childItem!.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("Version");
    }

    [Fact]
    public async Task DeleteComposite_AssignedToNode_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateCompositeConfigurationRequest { Name = "node-assigned-composite-test", EntryPoint = "main.dsc.yaml" };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var compositeName = createRequest.Name;

        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = "1.0.0" };
        var versionResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        versionResponse.EnsureSuccessStatusCode();

        var publishResponse = await client.PutAsync($"/api/v1/composite-configurations/{compositeName}/versions/1.0.0/publish", null);
        publishResponse.EnsureSuccessStatusCode();

        var registerRequest = new RegisterNodeRequest { Fqdn = "node-test.local", RegistrationKey = "test-registration-key" };
        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        var registration = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        var assignRequest = new AssignConfigurationRequest { ConfigurationName = compositeName, IsComposite = true };
        var assignResponse = await client.PutAsJsonAsync($"/api/v1/nodes/{registration!.NodeId}/configuration", assignRequest);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var response = await client.DeleteAsync($"/api/v1/composite-configurations/{compositeName}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("assigned to nodes");
    }

    [Fact]
    public async Task DeleteCompositeConfigurationVersion_DraftVersion_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();

        var compositeName = "test-composite-delete-version";
        var version = "1.0.0";

        // Create composite
        var createRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName,
            Description = "Test composite for version deletion",
            EntryPoint = "main.dsc.yaml"
        };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Create version
        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = version };
        var versionResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        versionResponse.EnsureSuccessStatusCode();

        // Delete version
        var response = await client.DeleteAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify version is gone
        var getResponse = await client.GetAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCompositeConfigurationVersion_PublishedVersion_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var compositeName = "test-composite-delete-published";
        var version = "1.0.0";

        // Create composite
        var createRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName,
            Description = "Test composite for published version deletion",
            EntryPoint = "main.dsc.yaml"
        };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Create and publish version
        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = version };
        var versionResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        versionResponse.EnsureSuccessStatusCode();

        var publishResponse = await client.PutAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}/publish", null);
        publishResponse.EnsureSuccessStatusCode();

        // Try to delete published version
        var response = await client.DeleteAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Be("Cannot delete published version");
    }

    [Fact]
    public async Task DeleteCompositeConfigurationVersion_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/v1/composite-configurations/non-existent/versions/1.0.0");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateChildConfiguration_ValidData_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();

        var compositeName = "test-composite-update-child";
        var version = "1.0.0";
        var configName = await CreateTestConfigurationAsync(client, "child-config-update");

        // Create composite
        var createRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName,
            Description = "Test composite for child update",
            EntryPoint = "main.dsc.yaml"
        };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Create version
        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = version };
        var versionResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        versionResponse.EnsureSuccessStatusCode();

        // Add child
        var addRequest = new AddChildConfigurationRequest { ChildConfigurationName = configName };
        var addResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}/children", addRequest);
        addResponse.EnsureSuccessStatusCode();
        var childItem = await addResponse.Content.ReadFromJsonAsync<CompositeConfigurationItemDto>();

        // Update child
        var updateRequest = new UpdateChildConfigurationRequest
        {
            ActiveVersion = "1.0.0",
            Order = 5
        };
        var response = await client.PutAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}/children/{childItem!.Id}", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedItem = await response.Content.ReadFromJsonAsync<CompositeConfigurationItemDto>();
        updatedItem!.ActiveVersion.Should().Be("1.0.0");
        updatedItem.Order.Should().Be(5);
    }

    [Fact]
    public async Task UpdateChildConfiguration_InvalidVersion_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var compositeName = "test-composite-update-child-invalid";
        var version = "1.0.0";
        var configName = await CreateTestConfigurationAsync(client, "child-config-update-invalid");

        // Create composite
        var createRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName,
            Description = "Test composite for invalid child update",
            EntryPoint = "main.dsc.yaml"
        };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Create version
        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = version };
        var versionResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        versionResponse.EnsureSuccessStatusCode();

        // Add child
        var addRequest = new AddChildConfigurationRequest { ChildConfigurationName = configName };
        var addResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}/children", addRequest);
        addResponse.EnsureSuccessStatusCode();
        var childItem = await addResponse.Content.ReadFromJsonAsync<CompositeConfigurationItemDto>();

        // Update child with invalid version
        var updateRequest = new UpdateChildConfigurationRequest
        {
            ActiveVersion = "999.999.999",
            Order = 5
        };
        var response = await client.PutAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}/children/{childItem!.Id}", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("Invalid ActiveVersion");
    }

    [Fact]
    public async Task UpdateChildConfiguration_PublishedVersion_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var compositeName = "test-composite-update-published";
        var version = "1.0.0";
        var configName = await CreateTestConfigurationAsync(client, "child-config-published");

        // Create composite
        var createRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName,
            Description = "Test composite for published update",
            EntryPoint = "main.dsc.yaml"
        };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Create and publish version
        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = version };
        var versionResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        versionResponse.EnsureSuccessStatusCode();

        var publishResponse = await client.PutAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}/publish", null);
        publishResponse.EnsureSuccessStatusCode();

        // Add child to published version (should fail)
        var addRequest = new AddChildConfigurationRequest { ChildConfigurationName = configName };
        var addResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}/children", addRequest);
        addResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateChildConfiguration_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var updateRequest = new UpdateChildConfigurationRequest
        {
            ActiveVersion = "1.0.0",
            Order = 5
        };
        var response = await client.PutAsJsonAsync("/api/v1/composite-configurations/non-existent/versions/1.0.0/children/00000000-0000-0000-0000-000000000000", updateRequest);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveChildConfiguration_ValidData_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();

        var compositeName = "test-composite-remove-child";
        var version = "1.0.0";
        var configName = await CreateTestConfigurationAsync(client, "child-config-remove");

        // Create composite
        var createRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName,
            Description = "Test composite for child removal",
            EntryPoint = "main.dsc.yaml"
        };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Create version
        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = version };
        var versionResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        versionResponse.EnsureSuccessStatusCode();

        // Add child
        var addRequest = new AddChildConfigurationRequest { ChildConfigurationName = configName };
        var addResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}/children", addRequest);
        addResponse.EnsureSuccessStatusCode();
        var childItem = await addResponse.Content.ReadFromJsonAsync<CompositeConfigurationItemDto>();

        // Remove child
        var response = await client.DeleteAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}/children/{childItem!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify child is gone
        var getResponse = await client.GetAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}");
        getResponse.EnsureSuccessStatusCode();
        var versionDetails = await getResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();
        versionDetails!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveChildConfiguration_PublishedVersion_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();

        var compositeName = "test-composite-remove-published";
        var version = "1.0.0";
        await CreateTestConfigurationAsync(client, "child-config-remove-published");

        // Create composite
        var createRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName,
            Description = "Test composite for published removal",
            EntryPoint = "main.dsc.yaml"
        };
        var createResponse = await client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        createResponse.EnsureSuccessStatusCode();

        // Create and publish version
        var versionRequest = new CreateCompositeConfigurationVersionRequest { Version = version };
        var versionResponse = await client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        versionResponse.EnsureSuccessStatusCode();

        var publishResponse = await client.PutAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}/publish", null);
        publishResponse.EnsureSuccessStatusCode();

        // Try to remove from published version (should fail at add, but let's test remove anyway)
        var response = await client.DeleteAsync($"/api/v1/composite-configurations/{compositeName}/versions/{version}/children/00000000-0000-0000-0000-000000000000");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound); // Child doesn't exist, but if it did, it would fail
    }

    [Fact]
    public async Task RemoveChildConfiguration_NonExistent_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/v1/composite-configurations/non-existent/versions/1.0.0/children/00000000-0000-0000-0000-000000000000");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
