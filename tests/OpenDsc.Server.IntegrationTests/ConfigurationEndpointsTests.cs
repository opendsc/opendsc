// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Data;
using OpenDsc.Server.Endpoints;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

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
        var response = await client.GetAsync("/api/v1/configurations", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConfigurations_WithAuth_ReturnsEmptyList()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/configurations", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var configs = await response.Content.ReadFromJsonAsync<List<ConfigurationSummaryDto>>(TestContext.Current.CancellationToken);
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

        var response = await client.PostAsync("/api/v1/configurations", content, TestContext.Current.CancellationToken);

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

        await client.PostAsync("/api/v1/configurations", content1, TestContext.Current.CancellationToken);

        using var content2 = new MultipartFormDataContent();
        content2.Add(new StringContent("duplicate-config"), "name");
        content2.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var file2 = new ByteArrayContent("content"u8.ToArray());
        file2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content2.Add(file2, "files", "main.dsc.yaml");

        var response = await client.PostAsync("/api/v1/configurations", content2, TestContext.Current.CancellationToken);

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
        await client.PostAsync("/api/v1/configurations", content, TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/v1/configurations/get-config-test", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<ConfigurationDetailsDto>(TestContext.Current.CancellationToken);
        config.Should().NotBeNull();
        config!.Name.Should().Be("get-config-test");
    }

    [Fact]
    public async Task GetConfiguration_NotFound_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/configurations/non-existent", TestContext.Current.CancellationToken);

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
        await client.PostAsync("/api/v1/configurations", content, TestContext.Current.CancellationToken);

        var response = await client.DeleteAsync("/api/v1/configurations/delete-config-test", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync("/api/v1/configurations/delete-config-test", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteConfiguration_NotFound_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/v1/configurations/non-existent", TestContext.Current.CancellationToken);

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
        await client.PostAsync("/api/v1/configurations", content1, TestContext.Current.CancellationToken);

        using var content2 = new MultipartFormDataContent();
        content2.Add(new StringContent("config2"), "name");
        content2.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var file2 = new ByteArrayContent("content2"u8.ToArray());
        file2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        content2.Add(file2, "files", "main.dsc.yaml");
        await client.PostAsync("/api/v1/configurations", content2, TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/v1/configurations", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var configs = await response.Content.ReadFromJsonAsync<List<ConfigurationSummaryDto>>(TestContext.Current.CancellationToken);
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
        await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);

        using var versionContent = new MultipartFormDataContent();
        versionContent.Add(new StringContent("2.0.0"), "version");
        var versionFile = new ByteArrayContent("v2content"u8.ToArray());
        versionFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        versionContent.Add(versionFile, "files", "main.dsc.yaml");

        var response = await client.PostAsync("/api/v1/configurations/version-test-config/versions", versionContent, TestContext.Current.CancellationToken);

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
        var createResponse = await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);
        var configDto = await createResponse.Content.ReadFromJsonAsync<ConfigurationDetailsDto>(TestContext.Current.CancellationToken);

        var response = await client.PutAsync($"/api/v1/configurations/publish-test-config/versions/{configDto!.LatestVersion}/publish", null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublishConfigurationVersion_FindsEntryPointInVersionDirectory()
    {
        using var client = CreateAuthenticatedClient();

        var configName = $"entrypoint-test-{Guid.NewGuid()}";
        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent(configName), "name");
        configContent.Add(new StringContent("config.dsc.yaml"), "entryPoint");
        configContent.Add(new StringContent("true"), "isDraft");

        // Create a configuration file with parameters block
        var configFileContent = @"
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
metadata:
  description: Test configuration
parameters:
  testParam:
    type: string
    defaultValue: test
resources: []
";
        var configFile = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(configFileContent));
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "config.dsc.yaml");

        var createResponse = await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var configDto = await createResponse.Content.ReadFromJsonAsync<ConfigurationDetailsDto>(TestContext.Current.CancellationToken);

        // Publish should succeed - entry point file should be found in v{version} directory
        var publishResponse = await client.PutAsync($"/api/v1/configurations/{configName}/versions/{configDto!.LatestVersion}/publish", null, TestContext.Current.CancellationToken);

        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);
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
        await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);

        using var v2Content = new MultipartFormDataContent();
        v2Content.Add(new StringContent("2.0.0"), "version");
        var v2File = new ByteArrayContent("v2"u8.ToArray());
        v2File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        v2Content.Add(v2File, "files", "main.dsc.yaml");
        await client.PostAsync("/api/v1/configurations/versions-list-test/versions", v2Content, TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/v1/configurations/versions-list-test/versions", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await response.Content.ReadFromJsonAsync<List<ConfigurationVersionDto>>(TestContext.Current.CancellationToken);
        versions.Should().NotBeNull();
        versions!.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task CreateConfigurationVersion_WithDifferentEntryPoint_ReturnsCreated()
    {
        using var client = CreateAuthenticatedClient();

        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent("version-entrypoint-test"), "name");
        configContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var configFile = new ByteArrayContent("v1content"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "main.dsc.yaml");
        await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);

        using var versionContent = new MultipartFormDataContent();
        versionContent.Add(new StringContent("2.0.0"), "version");
        versionContent.Add(new StringContent("main.bicep"), "entryPoint");
        var versionFile = new ByteArrayContent("bicep content"u8.ToArray());
        versionFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        versionContent.Add(versionFile, "files", "main.bicep");

        var response = await client.PostAsync("/api/v1/configurations/version-entrypoint-test/versions", versionContent, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var versionsResponse = await client.GetAsync("/api/v1/configurations/version-entrypoint-test/versions", TestContext.Current.CancellationToken);
        var versions = await versionsResponse.Content.ReadFromJsonAsync<List<ConfigurationVersionDto>>(TestContext.Current.CancellationToken);
        versions.Should().NotBeNull();
        var v1 = versions!.FirstOrDefault(v => v.Version == "1.0.0");
        var v2 = versions.FirstOrDefault(v => v.Version == "2.0.0");
        v1.Should().NotBeNull();
        v2.Should().NotBeNull();
        v1!.EntryPoint.Should().Be("main.dsc.yaml");
        v2!.EntryPoint.Should().Be("main.bicep");
    }

    [Fact]
    public async Task GetConfigurationVersions_ReturnsVersionsWithEntryPoint()
    {
        using var client = CreateAuthenticatedClient();

        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent("versions-entrypoint-list-test"), "name");
        configContent.Add(new StringContent("config.dsc.yaml"), "entryPoint");
        var configFile = new ByteArrayContent("v1"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "config.dsc.yaml");
        await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);

        var response = await client.GetAsync("/api/v1/configurations/versions-entrypoint-list-test/versions", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await response.Content.ReadFromJsonAsync<List<ConfigurationVersionDto>>(TestContext.Current.CancellationToken);
        versions.Should().NotBeNull();
        versions!.Should().HaveCount(1);
        versions[0].EntryPoint.Should().Be("config.dsc.yaml");
    }

    [Fact]
    public async Task CreateConfigurationVersion_InheritsEntryPointFromPreviousVersion()
    {
        using var client = CreateAuthenticatedClient();

        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent("inherit-entrypoint-test"), "name");
        configContent.Add(new StringContent("inherited.dsc.yaml"), "entryPoint");
        var configFile = new ByteArrayContent("v1content"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "inherited.dsc.yaml");
        await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);

        // Create v2 without specifying entryPoint - should inherit from v1
        using var versionContent = new MultipartFormDataContent();
        versionContent.Add(new StringContent("2.0.0"), "version");
        var versionFile = new ByteArrayContent("v2content"u8.ToArray());
        versionFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        versionContent.Add(versionFile, "files", "inherited.dsc.yaml");
        var createV2Response = await client.PostAsync("/api/v1/configurations/inherit-entrypoint-test/versions", versionContent, TestContext.Current.CancellationToken);
        createV2Response.StatusCode.Should().Be(HttpStatusCode.Created);

        var versionsResponse = await client.GetAsync("/api/v1/configurations/inherit-entrypoint-test/versions", TestContext.Current.CancellationToken);
        var versions = await versionsResponse.Content.ReadFromJsonAsync<List<ConfigurationVersionDto>>(TestContext.Current.CancellationToken);
        var v2 = versions!.FirstOrDefault(v => v.Version == "2.0.0");
        v2.Should().NotBeNull();
        v2!.EntryPoint.Should().Be("inherited.dsc.yaml");
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
        var createResponse = await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);

        using var v2Content = new MultipartFormDataContent();
        v2Content.Add(new StringContent("2.0.0"), "version");
        v2Content.Add(new StringContent("true"), "isDraft");
        var v2File = new ByteArrayContent("v2"u8.ToArray());
        v2File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        v2Content.Add(v2File, "files", "main.dsc.yaml");
        var v2Response = await client.PostAsync("/api/v1/configurations/delete-version-test/versions", v2Content, TestContext.Current.CancellationToken);
        var v2Dto = await v2Response.Content.ReadFromJsonAsync<ConfigurationVersionDto>(TestContext.Current.CancellationToken);

        var response = await client.DeleteAsync($"/api/v1/configurations/delete-version-test/versions/{v2Dto!.Version}", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task PublishConfiguration_WithBreakingParameterChanges_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();
        var configName = $"compat{Guid.NewGuid().ToString("N")}";

        // Create configuration with v1 parameter schema
        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent(configName), "name");
        configContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var configFile = new ByteArrayContent("resources: []"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "main.dsc.yaml");
        var configResponse = await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);
        configResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        configResponse.EnsureSuccessStatusCode();

        // Upload v1.0.0 parameter schema
        var v1Schema = @"{
  ""parameters"": {
    ""appName"": { ""type"": ""string"" }
  }
}";
        using var v1Request = new MultipartFormDataContent();
        v1Request.Add(new StringContent("1.0.0"), "version");
        var v1File = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(v1Schema));
        v1File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        v1Request.Add(v1File, "parametersFile", "parameters.json");
        var v1Response = await client.PutAsync($"/api/v1/configurations/{configName}/parameters", v1Request, TestContext.Current.CancellationToken);
        v1Response.EnsureSuccessStatusCode();

        // Create v1.1.0 schema with breaking changes (removing required parameter)
        var v1_1Schema = @"{
  ""parameters"": {
    ""newParam"": { ""type"": ""string"" }
  }
}";
        using var v1_1Request = new MultipartFormDataContent();
        v1_1Request.Add(new StringContent("1.1.0"), "version");
        var v1_1File = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(v1_1Schema));
        v1_1File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        v1_1Request.Add(v1_1File, "parametersFile", "parameters.json");
        var schemaResponse = await client.PutAsync($"/api/v1/configurations/{configName}/parameters", v1_1Request, TestContext.Current.CancellationToken);

        // Attempt to publish - should fail due to breaking changes
        schemaResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var publishResult = await schemaResponse.Content.ReadFromJsonAsync<PublishResultDto>(TestContext.Current.CancellationToken);
        publishResult.Should().NotBeNull();
        publishResult!.Success.Should().BeFalse();
        publishResult.CompatibilityReport.Should().NotBeNull();
        publishResult.CompatibilityReport!.HasBreakingChanges.Should().BeTrue();
    }

    [Fact]
    public async Task PublishConfiguration_WithNonBreakingParameterChanges_Succeeds()
    {
        using var client = CreateAuthenticatedClient();
        var configName = $"compat{Guid.NewGuid().ToString("N")}";

        // Create configuration with v1 parameter schema
        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent(configName), "name");
        configContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var configFile = new ByteArrayContent("resources: []"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "main.dsc.yaml");
        var configResponse = await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);
        configResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        configResponse.EnsureSuccessStatusCode();
        configResponse.EnsureSuccessStatusCode();

        // Upload v1.0.0 parameter schema
        var v1Schema = @"{
  ""parameters"": {
    ""appName"": { ""type"": ""string"" }
  }
}";
        using var v1Request = new MultipartFormDataContent();
        v1Request.Add(new StringContent("1.0.0"), "version");
        var v1File = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(v1Schema));
        v1File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        v1Request.Add(v1File, "parametersFile", "parameters.json");
        var v1Response = await client.PutAsync($"/api/v1/configurations/{configName}/parameters", v1Request, TestContext.Current.CancellationToken);
        v1Response.EnsureSuccessStatusCode();

        // Create v1.1.0 schema with non-breaking changes (adding optional parameter)
        var v1_1Schema = @"{
  ""parameters"": {
    ""appName"": { ""type"": ""string"" },
    ""port"": { ""type"": ""int"", ""defaultValue"": 8080 }
  }
}";
        using var v1_1Request = new MultipartFormDataContent();
        v1_1Request.Add(new StringContent("1.1.0"), "version");
        var v1_1File = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(v1_1Schema));
        v1_1File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        v1_1Request.Add(v1_1File, "parametersFile", "parameters.json");
        var schemaResponse = await client.PutAsync($"/api/v1/configurations/{configName}/parameters", v1_1Request, TestContext.Current.CancellationToken);

        // Should succeed - non-breaking changes allowed in minor version
        schemaResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublishConfiguration_BreakingChangesWithMajorVersionBump_Succeeds()
    {
        using var client = CreateAuthenticatedClient();
        var configName = $"compat{Guid.NewGuid().ToString("N")}";

        // Create configuration with v1 parameter schema
        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent(configName), "name");
        configContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var configFile = new ByteArrayContent("resources: []"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "main.dsc.yaml");
        await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);

        // Upload v1.0.0 parameter schema
        var v1Schema = @"{
  ""parameters"": {
    ""appName"": { ""type"": ""string"" }
  }
}";
        using var v1Request = new MultipartFormDataContent();
        v1Request.Add(new StringContent("1.0.0"), "version");
        var v1File = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(v1Schema));
        v1File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        v1Request.Add(v1File, "parametersFile", "parameters.json");
        await client.PutAsync($"/api/v1/configurations/{configName}/parameters", v1Request, TestContext.Current.CancellationToken);

        // Create v2.0.0 schema with breaking changes (major version bump)
        var v2Schema = @"{
  ""parameters"": {
    ""newParam"": { ""type"": ""string"" }
  }
}";
        using var v2Request = new MultipartFormDataContent();
        v2Request.Add(new StringContent("2.0.0"), "version");
        var v2File = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(v2Schema));
        v2File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        v2Request.Add(v2File, "parametersFile", "parameters.json");
        var schemaResponse = await client.PutAsync($"/api/v1/configurations/{configName}/parameters", v2Request, TestContext.Current.CancellationToken);

        // Should succeed - breaking changes allowed with major version bump
        schemaResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublishConfigurationVersion_WithCookieAuth_Succeeds()
    {
        using var client = CreateAuthenticatedClient();

        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent("cookie-auth-test-config"), "name");
        configContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        configContent.Add(new StringContent("true"), "isDraft");
        var configFile = new ByteArrayContent("content"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "main.dsc.yaml");
        var createResponse = await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);
        var configDto = await createResponse.Content.ReadFromJsonAsync<ConfigurationDetailsDto>(TestContext.Current.CancellationToken);

        var response = await client.PutAsync($"/api/v1/configurations/cookie-auth-test-config/versions/{configDto!.LatestVersion}/publish", null, TestContext.Current.CancellationToken);

        // Should succeed with cookie authentication
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PublishConfigurationVersion_ReturnsUpdatedVersionData()
    {
        // Root cause fix: Verify that the publish endpoint returns the updated isDraft status
        // This allows the UI to update immediately without requiring a database reload
        using var client = CreateAuthenticatedClient();
        var configName = $"publish-data-test-{Guid.NewGuid()}";

        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent(configName), "name");
        configContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        configContent.Add(new StringContent("true"), "isDraft");
        var configFile = new ByteArrayContent("resources: []"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "main.dsc.yaml");
        var createResponse = await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);
        var configDto = await createResponse.Content.ReadFromJsonAsync<ConfigurationDetailsDto>(TestContext.Current.CancellationToken);

        // Publish the draft version
        var publishResponse = await client.PutAsync(
            $"/api/v1/configurations/{configName}/versions/{configDto!.LatestVersion}/publish",
            null, TestContext.Current.CancellationToken);

        // Verify response status is OK
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify the response contains the updated version data
        var publishedVersion = await publishResponse.Content.ReadFromJsonAsync<ConfigurationVersionDto>(TestContext.Current.CancellationToken);
        publishedVersion.Should().NotBeNull();
        publishedVersion!.Version.Should().Be(configDto.LatestVersion);
        // This is the key fix: Status must be Published after publishing
        publishedVersion.Status.Should().Be(ConfigurationVersionStatus.Published);
    }


    [Fact]
    public async Task PublishConfigurationVersion_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        // Try to publish without authentication
        var response = await client.PutAsync("/api/v1/configurations/test-config/versions/1.0.0/publish", null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateConfigurationVersion_WithParameters_GeneratesSchema()
    {
        using var client = CreateAuthenticatedClient();
        var configName = $"schema-test-{Guid.NewGuid()}";

        // Create configuration
        using var createContent = new MultipartFormDataContent();
        createContent.Add(new StringContent(configName), "name");
        createContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");

        var yamlContent = @"
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources: []
parameters:
  appName:
    type: string
    description: Name of the application
  environment:
    type: string
    allowedValues: [dev, test, prod]
  port:
    type: int
    minValue: 1
    maxValue: 65535
";

        var mainFile = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(yamlContent));
        mainFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        createContent.Add(mainFile, "files", "main.dsc.yaml");

        var createResponse = await client.PostAsync("/api/v1/configurations", createContent, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Get the configuration details to verify parameter schema was created
        var detailsResponse = await client.GetAsync($"/api/v1/configurations/{configName}", TestContext.Current.CancellationToken);
        detailsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Query the database directly to verify ParameterSchema was created
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        var configuration = await db.Configurations
            .Include(c => c.Versions)
            .FirstOrDefaultAsync(c => c.Name == configName, TestContext.Current.CancellationToken);

        configuration.Should().NotBeNull();

        // Query parameter schemas for this configuration
        var schemas = await db.ParameterSchemas
            .Where(ps => ps.ConfigurationId == configuration!.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        schemas.Should().NotBeEmpty("Parameter schema should be auto-generated from YAML");

        var schema = schemas.First();
        schema.SchemaVersion.Should().Be("1.0.0", "Schema version should match configuration version");
        schema.GeneratedJsonSchema.Should().NotBeNullOrWhiteSpace("JSON schema should be generated");

        // Verify the JSON schema contains our parameters
        schema.GeneratedJsonSchema.Should().Contain("appName");
        schema.GeneratedJsonSchema.Should().Contain("environment");
        schema.GeneratedJsonSchema.Should().Contain("port");

        // Verify the configuration version is linked to the schema
        var version = configuration!.Versions.First();
        version.ParameterSchemaId.Should().Be(schema.Id, "Configuration version should be linked to parameter schema");
    }

    [Fact]
    public async Task CreateMultipleConfigurationVersions_WithParameters_GeneratesSeparateSchemas()
    {
        using var client = CreateAuthenticatedClient();
        var configName = $"multi-version-test-{Guid.NewGuid()}";

        // Create initial configuration with v1 parameters
        using var createContent = new MultipartFormDataContent();
        createContent.Add(new StringContent(configName), "name");
        createContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        createContent.Add(new StringContent("1.0.0"), "version");

        var v1Yaml = @"
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources: []
parameters:
  appName:
    type: string
";

        var mainFile = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(v1Yaml));
        mainFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        createContent.Add(mainFile, "files", "main.dsc.yaml");

        var createResponse = await client.PostAsync("/api/v1/configurations", createContent, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Create v2.0.0 with new parameters
        using var v2Content = new MultipartFormDataContent();
        v2Content.Add(new StringContent("2.0.0"), "version");

        var v2Yaml = @"
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources: []
parameters:
  appName:
    type: string
  environment:
    type: string
";

        var v2File = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(v2Yaml));
        v2File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        v2Content.Add(v2File, "files", "main.dsc.yaml");

        var v2Response = await client.PostAsync($"/api/v1/configurations/{configName}/versions", v2Content, TestContext.Current.CancellationToken);
        v2Response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verify both schema versions exist
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();

        var configuration = await db.Configurations
            .FirstOrDefaultAsync(c => c.Name == configName, TestContext.Current.CancellationToken);

        configuration.Should().NotBeNull();

        var schemas = await db.ParameterSchemas
            .Where(ps => ps.ConfigurationId == configuration!.Id)
            .ToListAsync(TestContext.Current.CancellationToken);

        schemas = schemas.OrderBy(ps => ps.SchemaVersion).ToList();

        schemas.Should().HaveCount(2, "Two parameter schemas should be generated");
        schemas[0].SchemaVersion.Should().Be("1.0.0");
        schemas[0].GeneratedJsonSchema.Should().Contain("appName");
        schemas[0].GeneratedJsonSchema.Should().NotContain("environment");

        schemas[1].SchemaVersion.Should().Be("2.0.0");
        schemas[1].GeneratedJsonSchema.Should().Contain("appName");
        schemas[1].GeneratedJsonSchema.Should().Contain("environment");
    }
}

public sealed class PublishResultDto
{
    public required bool Success { get; init; }
    public CompatibilityReportDto? CompatibilityReport { get; init; }
    public List<ParameterFileMigrationStatusDto>? MigrationRequirements { get; init; }
}

public sealed class CompatibilityReportDto
{
    public required bool HasBreakingChanges { get; init; }
    public required List<ParameterChangeDto> BreakingChanges { get; init; }
    public required List<ParameterChangeDto> NonBreakingChanges { get; init; }
}

public sealed class ParameterChangeDto
{
    public required string ParameterName { get; init; }
    public required string ChangeType { get; init; }
    public required string Details { get; init; }
}

public sealed class ParameterFileMigrationStatusDto
{
    public required string ScopeTypeName { get; init; }
    public string? ScopeValue { get; init; }
    public required string Version { get; init; }
    public required int MajorVersion { get; init; }
    public required bool NeedsMigration { get; init; }
    public List<ValidationErrorDto>? Errors { get; init; }
}

public sealed class ValidationErrorDto
{
    public required string Path { get; init; }
    public required string Message { get; init; }
    public required string Code { get; init; }
}
