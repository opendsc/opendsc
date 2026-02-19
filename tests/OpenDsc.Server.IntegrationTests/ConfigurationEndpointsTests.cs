// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Server.Endpoints;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class ConfigurationEndpointsTests : IDisposable
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
    public async Task GetConfigurations_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/configurations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConfigurations_WithAuth_ReturnsEmptyList()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/configurations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var configs = await response.Content.ReadFromJsonAsync<List<ConfigurationSummaryDto>>();
        configs.Should().NotBeNull();
        configs.Should().HaveCount(1);
        configs![0].Name.Should().Be("test-config");
    }

    [Fact]
    public async Task CreateConfiguration_WithValidData_ReturnsCreated()
    {
        using var client = CreateAuthenticatedClient();

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("create-test-config"), "name");
        content.Add(new StringContent("Test configuration"), "description");
        content.Add(new StringContent("main.dsc.yaml"), "entryPoint");

        var mainFile = new ByteArrayContent("$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json\nresources: []"u8.ToArray());
        mainFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(mainFile, "files", "main.dsc.yaml");

        var response = await client.PostAsync("/api/v1/configurations", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/configurations/create-test-config");
    }

    [Fact]
    public async Task CreateConfiguration_Duplicate_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();

        using var content1 = new MultipartFormDataContent();
        content1.Add(new StringContent("duplicate-config"), "name");
        content1.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var file1 = new ByteArrayContent("content"u8.ToArray());
        file1.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content1.Add(file1, "files", "main.dsc.yaml");

        await client.PostAsync("/api/v1/configurations", content1);

        using var content2 = new MultipartFormDataContent();
        content2.Add(new StringContent("duplicate-config"), "name");
        content2.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var file2 = new ByteArrayContent("content"u8.ToArray());
        file2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content2.Add(file2, "files", "main.dsc.yaml");

        var response = await client.PostAsync("/api/v1/configurations", content2);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetConfiguration_Existing_ReturnsConfiguration()
    {
        using var client = CreateAuthenticatedClient();

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("get-config-test"), "name");
        content.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var file = new ByteArrayContent("content"u8.ToArray());
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(file, "files", "main.dsc.yaml");
        await client.PostAsync("/api/v1/configurations", content);

        var response = await client.GetAsync("/api/v1/configurations/get-config-test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<ConfigurationDetailsDto>();
        config.Should().NotBeNull();
        config!.Name.Should().Be("get-config-test");
    }

    [Fact]
    public async Task GetConfiguration_NotFound_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/configurations/non-existent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteConfiguration_Existing_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("delete-config-test"), "name");
        content.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var file = new ByteArrayContent("content"u8.ToArray());
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content.Add(file, "files", "main.dsc.yaml");
        await client.PostAsync("/api/v1/configurations", content);

        var response = await client.DeleteAsync("/api/v1/configurations/delete-config-test");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync("/api/v1/configurations/delete-config-test");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteConfiguration_NotFound_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/v1/configurations/non-existent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConfigurations_AfterCreating_ReturnsAll()
    {
        using var client = CreateAuthenticatedClient();

        using var content1 = new MultipartFormDataContent();
        content1.Add(new StringContent("config1"), "name");
        content1.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var file1 = new ByteArrayContent("content1"u8.ToArray());
        file1.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content1.Add(file1, "files", "main.dsc.yaml");
        await client.PostAsync("/api/v1/configurations", content1);

        using var content2 = new MultipartFormDataContent();
        content2.Add(new StringContent("config2"), "name");
        content2.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var file2 = new ByteArrayContent("content2"u8.ToArray());
        file2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content2.Add(file2, "files", "main.dsc.yaml");
        await client.PostAsync("/api/v1/configurations", content2);

        var response = await client.GetAsync("/api/v1/configurations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var configs = await response.Content.ReadFromJsonAsync<List<ConfigurationSummaryDto>>();
        configs.Should().NotBeNull();
        configs!.Count.Should().BeGreaterThanOrEqualTo(2);
        configs.Should().Contain(c => c.Name == "config1");
        configs.Should().Contain(c => c.Name == "config2");
    }

    [Fact]
    public async Task CreateConfigurationVersion_WithValidData_ReturnsCreated()
    {
        using var client = CreateAuthenticatedClient();

        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent("version-test-config"), "name");
        configContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var configFile = new ByteArrayContent("initial"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "main.dsc.yaml");
        await client.PostAsync("/api/v1/configurations", configContent);

        using var versionContent = new MultipartFormDataContent();
        versionContent.Add(new StringContent("2.0.0"), "version");
        var versionFile = new ByteArrayContent("v2content"u8.ToArray());
        versionFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        versionContent.Add(versionFile, "files", "main.dsc.yaml");

        var response = await client.PostAsync("/api/v1/configurations/version-test-config/versions", versionContent);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PublishConfigurationVersion_ValidDraft_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();

        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent("publish-test-config"), "name");
        configContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        configContent.Add(new StringContent("true"), "isDraft");
        var configFile = new ByteArrayContent("content"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "main.dsc.yaml");
        var createResponse = await client.PostAsync("/api/v1/configurations", configContent);
        var configDto = await createResponse.Content.ReadFromJsonAsync<ConfigurationDetailsDto>();

        var response = await client.PutAsync($"/api/v1/configurations/publish-test-config/versions/{configDto!.LatestVersion}/publish", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetConfigurationVersions_ReturnsAllVersions()
    {
        using var client = CreateAuthenticatedClient();

        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent("versions-list-test"), "name");
        configContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var configFile = new ByteArrayContent("v1"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "main.dsc.yaml");
        await client.PostAsync("/api/v1/configurations", configContent);

        using var v2Content = new MultipartFormDataContent();
        v2Content.Add(new StringContent("2.0.0"), "version");
        var v2File = new ByteArrayContent("v2"u8.ToArray());
        v2File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        v2Content.Add(v2File, "files", "main.dsc.yaml");
        await client.PostAsync("/api/v1/configurations/versions-list-test/versions", v2Content);

        var response = await client.GetAsync("/api/v1/configurations/versions-list-test/versions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await response.Content.ReadFromJsonAsync<List<ConfigurationVersionDto>>();
        versions.Should().NotBeNull();
        versions!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task DeleteConfigurationVersion_DraftVersion_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();

        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent("delete-version-test"), "name");
        configContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var configFile = new ByteArrayContent("v1"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "main.dsc.yaml");
        var createResponse = await client.PostAsync("/api/v1/configurations", configContent);

        using var v2Content = new MultipartFormDataContent();
        v2Content.Add(new StringContent("2.0.0"), "version");
        v2Content.Add(new StringContent("true"), "isDraft");
        var v2File = new ByteArrayContent("v2"u8.ToArray());
        v2File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        v2Content.Add(v2File, "files", "main.dsc.yaml");
        var v2Response = await client.PostAsync("/api/v1/configurations/delete-version-test/versions", v2Content);
        var v2Dto = await v2Response.Content.ReadFromJsonAsync<ConfigurationVersionDto>();

        var response = await client.DeleteAsync($"/api/v1/configurations/delete-version-test/versions/{v2Dto!.Version}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
