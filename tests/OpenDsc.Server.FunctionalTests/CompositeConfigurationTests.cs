// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IO.Compression;
using System.Net;

using FluentAssertions;

using OpenDsc.Server.Contracts;
using OpenDsc.Server.FunctionalTests.DatabaseProviders;

using Xunit;

namespace OpenDsc.Server.FunctionalTests;

[Trait("Category", "Functional")]
public abstract class CompositeConfigurationTests
{
    protected readonly DatabaseProviderFixture Fixture;
    protected readonly HttpClient Client;

    protected CompositeConfigurationTests(DatabaseProviderFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateClient();
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");
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

        var createResponse = await Client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var getResponse = await Client.GetAsync($"/api/v1/composite-configurations/{compositeName}");
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
        await Client.PostAsJsonAsync("/api/v1/composite-configurations", createRequest);

        var versionRequest = new CreateCompositeConfigurationVersionRequest
        {
            Version = "1.0.0"
        };
        var versionResponse = await Client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        versionResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var versionDto = await versionResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();
        versionDto!.IsDraft.Should().BeTrue();

        var publishResponse = await Client.PostAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto.Id}/publish", null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getVersionResponse = await Client.GetAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto.Id}");
        var publishedVersion = await getVersionResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();
        publishedVersion!.IsDraft.Should().BeFalse();
    }

    [Fact]
    public async Task AddChildConfiguration_WorksAcrossProviders()
    {
        var compositeName = $"composite-child-{Guid.NewGuid()}";
        var childName = $"child-config-{Guid.NewGuid()}";

        var createChildRequest = new CreateConfigurationRequest
        {
            Name = childName,
            Content = @"$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources:
  - type: OpenDsc.Windows/Environment
    properties:
      name: TEST_VAR
      value: test_value
"
        };
        await Client.PostAsJsonAsync("/api/v1/configurations", createChildRequest);

        var createCompositeRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName
        };
        await Client.PostAsJsonAsync("/api/v1/composite-configurations", createCompositeRequest);

        var versionRequest = new CreateCompositeConfigurationVersionRequest
        {
            Version = "1.0.0"
        };
        var versionResponse = await Client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        var versionDto = await versionResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();

        var addChildRequest = new AddChildConfigurationRequest
        {
            ChildConfigurationName = childName
        };
        var addChildResponse = await Client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto!.Id}/children", addChildRequest);
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

        var createChild1Request = new CreateConfigurationRequest
        {
            Name = childName1,
            Content = @"$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources:
  - type: OpenDsc.Windows/Environment
    properties:
      name: CHILD1_VAR
      value: child1_value
"
        };
        await Client.PostAsJsonAsync("/api/v1/configurations", createChild1Request);

        var createChild2Request = new CreateConfigurationRequest
        {
            Name = childName2,
            Content = @"$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources:
  - type: OpenDsc.Windows/Environment
    properties:
      name: CHILD2_VAR
      value: child2_value
"
        };
        await Client.PostAsJsonAsync("/api/v1/configurations", createChild2Request);

        var createCompositeRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName
        };
        await Client.PostAsJsonAsync("/api/v1/composite-configurations", createCompositeRequest);

        var versionRequest = new CreateCompositeConfigurationVersionRequest
        {
            Version = "1.0.0"
        };
        var versionResponse = await Client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        var versionDto = await versionResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();

        var addChild1Request = new AddChildConfigurationRequest
        {
            ChildConfigurationName = childName1
        };
        await Client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto!.Id}/children", addChild1Request);

        var addChild2Request = new AddChildConfigurationRequest
        {
            ChildConfigurationName = childName2
        };
        await Client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto.Id}/children", addChild2Request);

        await Client.PostAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto.Id}/publish", null);

        var fqdn = $"bundle-test-{Guid.NewGuid()}.example.com";
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = fqdn,
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await Client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = compositeName,
            IsComposite = true
        };
        await Client.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

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

        mainContent.Should().Contain($"- !include {childName1}/main.dsc.yaml");
        mainContent.Should().Contain($"- !include {childName2}/main.dsc.yaml");
    }

    [Fact]
    public async Task CompositeChecksum_ConsistentAcrossProviders()
    {
        var compositeName = $"composite-checksum-{Guid.NewGuid()}";
        var childName = $"child-checksum-{Guid.NewGuid()}";

        var createChildRequest = new CreateConfigurationRequest
        {
            Name = childName,
            Content = @"$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources: []
"
        };
        await Client.PostAsJsonAsync("/api/v1/configurations", createChildRequest);

        var createCompositeRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName
        };
        await Client.PostAsJsonAsync("/api/v1/composite-configurations", createCompositeRequest);

        var versionRequest = new CreateCompositeConfigurationVersionRequest
        {
            Version = "1.0.0"
        };
        var versionResponse = await Client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        var versionDto = await versionResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();

        var addChildRequest = new AddChildConfigurationRequest
        {
            ChildConfigurationName = childName
        };
        await Client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto!.Id}/children", addChildRequest);

        await Client.PostAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto.Id}/publish", null);

        var fqdn = $"checksum-test-{Guid.NewGuid()}.example.com";
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = fqdn,
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await Client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = compositeName,
            IsComposite = true
        };
        await Client.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

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

        var createChildRequest = new CreateConfigurationRequest
        {
            Name = childName,
            Content = "test content"
        };
        await Client.PostAsJsonAsync("/api/v1/configurations", createChildRequest);

        var createCompositeRequest = new CreateCompositeConfigurationRequest
        {
            Name = compositeName
        };
        await Client.PostAsJsonAsync("/api/v1/composite-configurations", createCompositeRequest);

        var versionRequest = new CreateCompositeConfigurationVersionRequest
        {
            Version = "1.0.0"
        };
        var versionResponse = await Client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions", versionRequest);
        var versionDto = await versionResponse.Content.ReadFromJsonAsync<CompositeConfigurationVersionDto>();

        var addChildRequest = new AddChildConfigurationRequest
        {
            ChildConfigurationName = childName
        };
        await Client.PostAsJsonAsync($"/api/v1/composite-configurations/{compositeName}/versions/{versionDto!.Id}/children", addChildRequest);

        var deleteResponse = await Client.DeleteAsync($"/api/v1/composite-configurations/{compositeName}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await Client.GetAsync($"/api/v1/composite-configurations/{compositeName}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var getChildResponse = await Client.GetAsync($"/api/v1/configurations/{childName}");
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
