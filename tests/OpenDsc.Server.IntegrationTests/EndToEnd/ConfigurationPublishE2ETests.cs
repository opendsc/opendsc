// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Contracts.Configurations;

using Xunit;

namespace OpenDsc.Server.IntegrationTests.EndToEnd;

[Trait("Category", "Integration")]
[Trait("Type", "E2E")]
public class ConfigurationPublishE2ETests : IDisposable
{
    private readonly ServerWebApplicationFactory _factory = new();

    public void Dispose()
    {
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CompletePublishFlow_WithCookieAuth_SucceedsEndToEnd()
    {
        using var client = _factory.CreateAuthenticatedClient();

        // Step 1: Create a draft configuration
        var configName = $"e2e-publish-test-{Guid.NewGuid()}";
        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent(configName), "name");
        configContent.Add(new StringContent("Test E2E Configuration"), "description");
        configContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");

        var configFileContent = @"
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
metadata:
  description: E2E test configuration
resources:
  - type: OpenDsc.Windows/Environment
    properties:
      name: TEST_VAR
      value: test
";
        var configFile = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(configFileContent));
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "main.dsc.yaml");

        var createResponse = await client.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Step 2: Verify configuration was created as draft
        var getResponse = await client.GetAsync($"/api/v1/configurations/{configName}", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var configDto = await getResponse.Content.ReadFromJsonAsync<ConfigurationDetails>(TestContext.Current.CancellationToken);
        configDto.Should().NotBeNull();
        configDto!.LatestVersion.Should().Be("1.0.0");

        var versionResponse = await client.GetAsync($"/api/v1/configurations/{configName}/versions", TestContext.Current.CancellationToken);
        versionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await versionResponse.Content.ReadFromJsonAsync<List<ConfigurationVersionDetails>>(TestContext.Current.CancellationToken);
        versions.Should().NotBeNull();
        versions.Should().HaveCount(1);
        versions![0].Status.Should().Be(ConfigurationVersionStatus.Draft);

        // Step 3: Publish the version (this is where cookie auth forwarding is critical)
        var publishResponse = await client.PutAsync($"/api/v1/configurations/{configName}/versions/1.0.0/publish", null, TestContext.Current.CancellationToken);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 4: Verify the version is now published
        var verifyVersionResponse = await client.GetAsync($"/api/v1/configurations/{configName}/versions", TestContext.Current.CancellationToken);
        verifyVersionResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var publishedVersions = await verifyVersionResponse.Content.ReadFromJsonAsync<List<ConfigurationVersionDetails>>(TestContext.Current.CancellationToken);
        publishedVersions.Should().NotBeNull();
        publishedVersions.Should().HaveCount(1);
        publishedVersions![0].Status.Should().Be(ConfigurationVersionStatus.Published);

        // Step 5: Verify configuration details show published version
        var finalGetResponse = await client.GetAsync($"/api/v1/configurations/{configName}", TestContext.Current.CancellationToken);
        finalGetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var finalConfigDto = await finalGetResponse.Content.ReadFromJsonAsync<ConfigurationDetails>(TestContext.Current.CancellationToken);
        finalConfigDto.Should().NotBeNull();
        finalConfigDto!.LatestVersion.Should().Be("1.0.0");
    }

    [Fact]
    public async Task PublishFlow_WithoutAuthentication_FailsAtPublishStep()
    {
        // Create authenticated client for setup
        using var setupClient = _factory.CreateAuthenticatedClient();

        var configName = $"e2e-unauth-test-{Guid.NewGuid()}";
        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent(configName), "name");
        configContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var configFile = new ByteArrayContent("content"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "main.dsc.yaml");

        await setupClient.PostAsync("/api/v1/configurations", configContent, TestContext.Current.CancellationToken);

        // Try to publish without authentication
        using var unauthClient = _factory.CreateClient();
        var publishResponse = await unauthClient.PutAsync($"/api/v1/configurations/{configName}/versions/1.0.0/publish", null, TestContext.Current.CancellationToken);

        publishResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PublishFlow_MultipleVersions_AllUseCorrectAuthentication()
    {
        using var client = _factory.CreateAuthenticatedClient();

        var configName = $"e2e-multi-version-{Guid.NewGuid()}";

        // Create initial draft configuration
        using var initialContent = new MultipartFormDataContent();
        initialContent.Add(new StringContent(configName), "name");
        initialContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var initialFile = new ByteArrayContent("v1"u8.ToArray());
        initialFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        initialContent.Add(initialFile, "files", "main.dsc.yaml");
        await client.PostAsync("/api/v1/configurations", initialContent, TestContext.Current.CancellationToken);

        // Publish version 1.0.0
        var publish1Response = await client.PutAsync($"/api/v1/configurations/{configName}/versions/1.0.0/publish", null, TestContext.Current.CancellationToken);
        publish1Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Create version 2.0.0 as draft
        using var v2Content = new MultipartFormDataContent();
        v2Content.Add(new StringContent("2.0.0"), "version");
        var v2File = new ByteArrayContent("v2"u8.ToArray());
        v2File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        v2Content.Add(v2File, "files", "main.dsc.yaml");
        await client.PostAsync($"/api/v1/configurations/{configName}/versions", v2Content, TestContext.Current.CancellationToken);

        // Publish version 2.0.0
        var publish2Response = await client.PutAsync($"/api/v1/configurations/{configName}/versions/2.0.0/publish", null, TestContext.Current.CancellationToken);
        publish2Response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify both versions are published
        var versionsResponse = await client.GetAsync($"/api/v1/configurations/{configName}/versions", TestContext.Current.CancellationToken);
        var versions = await versionsResponse.Content.ReadFromJsonAsync<List<ConfigurationVersionDetails>>(TestContext.Current.CancellationToken);
        versions.Should().NotBeNull();
        versions.Should().HaveCount(2);
        versions!.All(v => v.Status == ConfigurationVersionStatus.Published).Should().BeTrue();
    }
}
