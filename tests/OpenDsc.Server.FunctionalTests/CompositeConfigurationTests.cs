// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IO.Compression;
using System.Net;

using AwesomeAssertions;

using OpenDsc.Server.Contracts;
using OpenDsc.Server.FunctionalTests.DatabaseProviders;

using Xunit;

namespace OpenDsc.Server.FunctionalTests;

[Trait("Category", "Functional")]
public abstract class CompositeConfigurationTests : IAsyncLifetime
{
    protected readonly DatabaseProviderFixture Fixture;
    protected readonly HttpClient Client;
    protected HttpClient AuthClient = null!;

    protected CompositeConfigurationTests(DatabaseProviderFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateClient();
    }

    public async Task InitializeAsync()
    {
        AuthClient = await AuthenticationHelper.CreateAuthenticatedClientAsync(Fixture);
    }

    public Task DisposeAsync()
    {
        AuthClient?.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateCompositeConfiguration_WorksAcrossAllProviders()
    {
        var compositeName = $"composite-{Guid.NewGuid()}";

        var createRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName,
            Description = "Test composite configuration"
        };

        var createResponse = await AuthClient.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var getResponse = await AuthClient.GetAsync($"/api/v1/composite-configurations/{compositeName}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var composite = await getResponse.Content.ReadFromJsonAsync<CompositeConfigurationDetailsDto>();
        composite.Should().NotBeNull();
        composite!.Name.Should().Be(compositeName);
        composite.Description.Should().Be("Test composite configuration");
    }

    [Fact]
    public async Task CreateVersionAndPublish_WorksCorrectly()
    {
        var compositeName = $"composite-version-{Guid.NewGuid()}";

        var createRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName
        };
        await AuthClient.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);

        var versionRequest = new CreateCompositeConfigurationVersionRequest
        {
            Version = "1.0.0"
        };
        var versionResponse = await AuthClient.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        versionResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var versionDto = await versionResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();
        versionDto!.IsDraft.Should().BeTrue();

        var publishResponse = await AuthClient.PutAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto.Version}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getVersionResponse = await AuthClient.GetAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto.Version}");
        var publishedVersion = await getVersionResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();
        publishedVersion!.IsDraft.Should().BeFalse();
    }

    [Fact]
    public async Task AddChildConfiguration_WorksAcrossProviders()
    {
        var compositeName = $"composite-child-{Guid.NewGuid()}";
        var childName = $"child-config-{Guid.NewGuid()}";

        using var childContent = new MultipartFormDataContent();
        childContent.Add(new StringContent(childName), "name");
        childContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var childFile = new ByteArrayContent("$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json\nresources:\n  - type: OpenDsc.Windows/Environment\n    properties:\n      name: TEST_VAR\n      value: test_value\n"u8.ToArray());
        childFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        childContent.Add(childFile, "files", "main.dsc.yaml");
        await AuthClient.PostAsync("/api/v1/configurations", childContent);

        var createCompositeRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName
        };
        await AuthClient.PostAsJsonAsync("/api/v1/composite-configurations", createCompositeRequest);

        var versionRequest = new CreateCompositeConfigurationVersionRequest
        {
            Version = "1.0.0"
        };
        var versionResponse = await AuthClient.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        var versionDto = await versionResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();

        var addChildRequest = new AddChildConfigurationRequest
        {
            ChildConfigurationName = childName
        };
        var addChildResponse = await AuthClient.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto!.Version}/children", addChildRequest);
        addChildResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var childDto = await addChildResponse.Content.ReadFromJsonAsync<CompositeConfigurationItemDto>();
        childDto.Should().NotBeNull();
        childDto!.ChildConfigurationName.Should().Be(childName);
    }

    [Fact]
    public async Task CompositeBundle_GeneratesCorrectStructure()
    {
        var compositeName = $"composite-bundle-{Guid.NewGuid()}";
        var childName1 = $"child1-{Guid.NewGuid()}";
        var childName2 = $"child2-{Guid.NewGuid()}";

        using var child1Content = new MultipartFormDataContent();
        child1Content.Add(new StringContent(childName1), "name");
        child1Content.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var child1File = new ByteArrayContent("$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json\nresources:\n  - type: OpenDsc.Windows/Environment\n    properties:\n      name: CHILD1_VAR\n      value: child1_value\n"u8.ToArray());
        child1File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        child1Content.Add(child1File, "files", "main.dsc.yaml");
        await AuthClient.PostAsync("/api/v1/configurations", child1Content);

        using var child2Content = new MultipartFormDataContent();
        child2Content.Add(new StringContent(childName2), "name");
        child2Content.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var child2File = new ByteArrayContent("$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json\nresources:\n  - type: OpenDsc.Windows/Environment\n    properties:\n      name: CHILD2_VAR\n      value: child2_value\n"u8.ToArray());
        child2File.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        child2Content.Add(child2File, "files", "main.dsc.yaml");
        await AuthClient.PostAsync("/api/v1/configurations", child2Content);

        var createCompositeRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName
        };
        await AuthClient.PostAsJsonAsync("/api/v1/composite-configurations", createCompositeRequest);

        var versionRequest = new CreateCompositeConfigurationVersionRequest
        {
            Version = "1.0.0"
        };
        var versionResponse = await AuthClient.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        var versionDto = await versionResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();

        var addChild1Request = new AddChildConfigurationRequest
        {
            ChildConfigurationName = childName1,
            Order = 0
        };
        await AuthClient.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto!.Version}/children", addChild1Request);

        var addChild2Request = new AddChildConfigurationRequest
        {
            ChildConfigurationName = childName2,
            Order = 1
        };
        await AuthClient.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto.Version}/children", addChild2Request);

        await AuthClient.PutAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto.Version}/publish", null);

        var fqdn = $"bundle-test-{Guid.NewGuid()}.example.com";
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = fqdn,
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await AuthClient.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = compositeName,
            IsComposite = true
        };
        await AuthClient.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        using var nodeClient = Fixture.CreateClient();
        var bundleResponse = await nodeClient.GetAsync($"/api/v1/nodes/{registerResult.NodeId}/configuration/bundle");
        bundleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var bundleStream = await bundleResponse.Content.ReadAsStreamAsync();
        using var archive = new ZipArchive(bundleStream, ZipArchiveMode.Read);

        var entries = archive.Entries.Select(e => e.FullName).ToList();

        entries.Should().Contain("main.dsc.yaml");
        entries.Should().Contain($"{childName1}/main.dsc.yaml");
        entries.Should().Contain($"{childName2}/main.dsc.yaml");

        var mainEntry = archive.GetEntry("main.dsc.yaml");
        mainEntry.Should().NotBeNull();

        using var mainStream = mainEntry!.Open();
        using var mainReader = new StreamReader(mainStream);
        var mainContent = await mainReader.ReadToEndAsync();

        mainContent.Should().Contain("type: Microsoft.DSC/Include");
        mainContent.Should().Contain($"configurationFile: {childName1}/main.dsc.yaml");
        mainContent.Should().Contain($"configurationFile: {childName2}/main.dsc.yaml");
    }

    [Fact]
    public async Task CompositeChecksum_ConsistentAcrossProviders()
    {
        var compositeName = $"composite-checksum-{Guid.NewGuid()}";
        var childName = $"child-checksum-{Guid.NewGuid()}";

        using var childContent = new MultipartFormDataContent();
        childContent.Add(new StringContent(childName), "name");
        childContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var childFile = new ByteArrayContent("$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json\nresources: []\n"u8.ToArray());
        childFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        childContent.Add(childFile, "files", "main.dsc.yaml");
        await AuthClient.PostAsync("/api/v1/configurations", childContent);

        var createCompositeRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName
        };
        await AuthClient.PostAsJsonAsync("/api/v1/composite-configurations", createCompositeRequest);

        var versionRequest = new CreateCompositeConfigurationVersionRequest
        {
            Version = "1.0.0"
        };
        var versionResponse = await AuthClient.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        var versionDto = await versionResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();

        var addChildRequest = new AddChildConfigurationRequest
        {
            ChildConfigurationName = childName
        };
        await AuthClient.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto!.Version}/children", addChildRequest);

        await AuthClient.PutAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto.Version}/publish", null);

        var fqdn = $"checksum-test-{Guid.NewGuid()}.example.com";
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = fqdn,
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await AuthClient.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = compositeName,
            IsComposite = true
        };
        await AuthClient.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        using var nodeClient = Fixture.CreateClient();
        var checksumResponse = await nodeClient.GetAsync($"/api/v1/nodes/{registerResult.NodeId}/configuration/checksum");
        checksumResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var checksumResult = await checksumResponse.Content.ReadFromJsonAsync<ConfigurationChecksumResponse>();
        checksumResult.Should().NotBeNull();
        checksumResult!.Checksum.Should().NotBeNullOrEmpty();
        checksumResult.Checksum.Length.Should().Be(64);
    }

    [Fact]
    public async Task DeleteCompositeConfiguration_CascadesCorrectly()
    {
        var compositeName = $"composite-delete-{Guid.NewGuid()}";
        var childName = $"child-delete-{Guid.NewGuid()}";

        using var childContent = new MultipartFormDataContent();
        childContent.Add(new StringContent(childName), "name");
        childContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var childFile = new ByteArrayContent("test content"u8.ToArray());
        childFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        childContent.Add(childFile, "files", "main.dsc.yaml");
        await AuthClient.PostAsync("/api/v1/configurations", childContent);

        var createCompositeRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName
        };
        await AuthClient.PostAsJsonAsync("/api/v1/composite-configurations", createCompositeRequest);

        var versionRequest = new CreateCompositeConfigurationVersionRequest
        {
            Version = "1.0.0"
        };
        var versionResponse = await AuthClient.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        var versionDto = await versionResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();

        var addChildRequest = new AddChildConfigurationRequest
        {
            ChildConfigurationName = childName
        };
        await AuthClient.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto!.Version}/children", addChildRequest);

        var deleteResponse = await AuthClient.DeleteAsync($"/api/v1/composite-configurations/{compositeName}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await AuthClient.GetAsync($"/api/v1/composite-configurations/{compositeName}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var getChildResponse = await AuthClient.GetAsync($"/api/v1/configurations/{childName}");
        getChildResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

[Collection("SQLite")]
public class SqliteCompositeConfigurationTests(SqliteProviderFixture fixture) : CompositeConfigurationTests(fixture)
{
}

[Collection("SQL Server")]
public class SqlServerCompositeConfigurationTests(SqlServerProviderFixture fixture) : CompositeConfigurationTests(fixture)
{
}

[Collection("PostgreSQL")]
public class PostgreSqlCompositeConfigurationTests(PostgreSqlProviderFixture fixture) : CompositeConfigurationTests(fixture)
{
}
