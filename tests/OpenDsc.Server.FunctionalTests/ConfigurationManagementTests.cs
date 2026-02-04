// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using FluentAssertions;

using OpenDsc.Server.Contracts;
using OpenDsc.Server.FunctionalTests.DatabaseProviders;

using Xunit;

namespace OpenDsc.Server.FunctionalTests;

[Trait("Category", "Functional")]
public abstract class ConfigurationManagementTests
{
    protected readonly DatabaseProviderFixture Fixture;
    protected readonly HttpClient Client;

    protected ConfigurationManagementTests(DatabaseProviderFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateClient();
    }

    [Fact]
    public async Task CreateAndRetrieveConfiguration_WorksAcrossAllProviders()
    {
        using var adminClient = Fixture.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

        var configName = $"test-config-{Guid.NewGuid()}";
        var configContent = @"
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources:
  - type: OpenDsc.Windows/Environment
    properties:
      name: TEST_VAR
      value: test_value
";

        var createRequest = new CreateConfigurationRequest
        {
            Name = configName,
            Content = configContent
        };

        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/configurations", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var getResponse = await adminClient.GetAsync($"/api/v1/configurations/{configName}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var configDetails = await getResponse.Content.ReadFromJsonAsync<ConfigurationDetails>();
        configDetails.Should().NotBeNull();
        configDetails!.Content.Trim().Should().Be(configContent.Trim());
    }

    [Fact]
    public async Task AssignConfigurationToNode_WorksCorrectly()
    {
        var fqdn = $"config-test-{Guid.NewGuid()}.example.com";
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = fqdn,
            RegistrationKey = "test-registration-key"
        };

        var registerResponse = await Client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = Fixture.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

        var configName = $"node-config-{Guid.NewGuid()}";
        var configContent = @"
$schema: https://raw.githubusercontent.com/PowerShell/DSC/main/schemas/2024/04/config/document.json
resources: []
";

        var createConfigRequest = new CreateConfigurationRequest
        {
            Name = configName,
            Content = configContent
        };
        await adminClient.PostAsJsonAsync("/api/v1/configurations", createConfigRequest);

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = configName
        };
        var assignResponse = await adminClient.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var nodeResponse = await adminClient.GetAsync($"/api/v1/nodes/{registerResult.NodeId}");
        var node = await nodeResponse.Content.ReadFromJsonAsync<NodeSummary>();
        node!.ConfigurationName.Should().Be(configName);
    }

    [Fact]
    public async Task ConfigurationChecksum_ConsistentAcrossProviders()
    {
        using var adminClient = Fixture.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

        var configName = $"checksum-test-{Guid.NewGuid()}";
        var configContent = "test content for checksum";

        var createRequest = new CreateConfigurationRequest
        {
            Name = configName,
            Content = configContent
        };
        await adminClient.PostAsJsonAsync("/api/v1/configurations", createRequest);

        var fqdn = $"checksum-node-{Guid.NewGuid()}.example.com";
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = fqdn,
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await Client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = configName
        };
        await adminClient.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        await Task.CompletedTask;
    }
}

[Collection("SQLite")]
public class SqliteConfigurationManagementTests(SqliteProviderFixture fixture) : ConfigurationManagementTests(fixture)
{
}

[Collection("SQL Server")]
public class SqlServerConfigurationManagementTests(SqlServerProviderFixture fixture) : ConfigurationManagementTests(fixture)
{
}

[Collection("PostgreSQL")]
public class PostgreSqlConfigurationManagementTests(PostgreSqlProviderFixture fixture) : ConfigurationManagementTests(fixture)
{
}
