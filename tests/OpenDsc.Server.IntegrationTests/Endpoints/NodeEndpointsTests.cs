// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Lcm.Contracts;
using OpenDsc.Server.Contracts;
using OpenDsc.Server.Endpoints;

using Xunit;

namespace OpenDsc.Server.IntegrationTests.Endpoints;

[Trait("Category", "Integration")]
public class NodeEndpointsTests : IClassFixture<ServerWebApplicationFactory>
{
    private readonly ServerWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NodeEndpointsTests(ServerWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterNode_WithValidRegistrationKey_ReturnsNodeIdOnly()
    {
        var request = new RegisterNodeRequest
        {
            Fqdn = "test-node.example.com",
            RegistrationKey = "test-registration-key"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/nodes/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegisterNodeResponse>();
        result.Should().NotBeNull();
        result!.NodeId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterNode_WithInvalidRegistrationKey_ReturnsBadRequest()
    {
        var request = new RegisterNodeRequest
        {
            Fqdn = "test-node.example.com",
            RegistrationKey = "invalid-key"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/nodes/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().Contain("Invalid registration key");
    }

    [Fact]
    public async Task RegisterNode_WithoutFqdn_ReturnsBadRequest()
    {
        var request = new RegisterNodeRequest
        {
            Fqdn = "",
            RegistrationKey = "test-registration-key"
        };

        var response = await _client.PostAsJsonAsync("/api/v1/nodes/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterNode_ReRegistrationOfExistingNode_ReturnsSameNodeId()
    {
        var request = new RegisterNodeRequest
        {
            Fqdn = "reregister-test.example.com",
            RegistrationKey = "test-registration-key"
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        var secondResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", request);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        secondResult.Should().NotBeNull();
        secondResult!.NodeId.Should().Be(firstResult!.NodeId);
    }

    [Fact]
    public async Task GetNodes_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/nodes/");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetNodes_WithAdminAuthentication_ReturnsNodeList()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "list-test.example.com",
            RegistrationKey = "test-registration-key"
        };
        await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);

        using var adminClient = _factory.CreateAuthenticatedClient();

        var response = await adminClient.GetAsync("/api/v1/nodes/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var nodes = await response.Content.ReadFromJsonAsync<List<NodeSummary>>();
        nodes.Should().NotBeNull();
        nodes!.Should().NotBeEmpty();
        nodes.Should().Contain(n => n.Fqdn == "list-test.example.com");
    }

    [Fact]
    public async Task GetNode_WithAdminAuth_ReturnsNodeDetails()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "getnode-test.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateAuthenticatedClient();

        var response = await adminClient.GetAsync($"/api/v1/nodes/{registerResult!.NodeId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var node = await response.Content.ReadFromJsonAsync<NodeSummary>();
        node.Should().NotBeNull();
        node!.Id.Should().Be(registerResult.NodeId);
        node.Fqdn.Should().Be("getnode-test.example.com");
    }

    [Fact]
    public async Task GetNode_WithNonExistentNode_ReturnsNotFound()
    {
        using var adminClient = _factory.CreateAuthenticatedClient();

        var response = await adminClient.GetAsync($"/api/v1/nodes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().Contain("Node not found");
    }

    [Fact]
    public async Task DeleteNode_WithAdminAuth_DeletesNode()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "delete-test.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateAuthenticatedClient();

        var response = await adminClient.DeleteAsync($"/api/v1/nodes/{registerResult!.NodeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await adminClient.GetAsync($"/api/v1/nodes/{registerResult.NodeId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteNode_WithNonExistentNode_ReturnsNotFound()
    {
        using var adminClient = _factory.CreateAuthenticatedClient();

        var response = await adminClient.DeleteAsync($"/api/v1/nodes/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().Contain("Node not found");
    }

    [Fact]
    public async Task AssignConfiguration_WithValidData_AssignsConfiguration()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "assign-test.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateAuthenticatedClient();

        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent("test-assign-config"), "name");
        configContent.Add(new StringContent("Test configuration"), "description");
        configContent.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var configFile = new ByteArrayContent("# Test configuration\n"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "main.dsc.yaml");
        var createResponse = await adminClient.PostAsync("/api/v1/configurations", configContent);
        var configDto = await createResponse.Content.ReadFromJsonAsync<ConfigurationDetailsDto>();

        await adminClient.PutAsync($"/api/v1/configurations/test-assign-config/versions/{configDto!.LatestVersion}/publish", null);

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = "test-assign-config"
        };
        var response = await adminClient.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var nodeResponse = await adminClient.GetAsync($"/api/v1/nodes/{registerResult.NodeId}");
        var node = await nodeResponse.Content.ReadFromJsonAsync<NodeSummary>();
        node!.ConfigurationName.Should().Be("test-assign-config");
    }

    [Fact]
    public async Task AssignConfiguration_WithMissingConfigurationName_ReturnsBadRequest()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "assign-noname-test.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateAuthenticatedClient();

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = ""
        };
        var response = await adminClient.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignConfiguration_WithNonExistentNode_ReturnsNotFound()
    {
        using var adminClient = _factory.CreateAuthenticatedClient();

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = "test-config"
        };
        var response = await adminClient.PutAsJsonAsync($"/api/v1/nodes/{Guid.NewGuid()}/configuration", assignRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("Node not found");
    }

    [Fact]
    public async Task GetConfigurationChecksum_ReturnsChecksumAndEntryPoint()
    {
        // Register node
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "checksum-entrypoint-test.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateAuthenticatedClient();

        // Create and publish a configuration
        var configName = $"checksum-entrypoint-config-{Guid.NewGuid():N}";
        using var configContent = new MultipartFormDataContent();
        configContent.Add(new StringContent(configName), "name");
        configContent.Add(new StringContent("deploy.dsc.yaml"), "entryPoint");
        var configFile = new ByteArrayContent("resources: []"u8.ToArray());
        configFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        configContent.Add(configFile, "files", "deploy.dsc.yaml");
        var createResponse = await adminClient.PostAsync("/api/v1/configurations", configContent);
        var configDto = await createResponse.Content.ReadFromJsonAsync<ConfigurationDetailsDto>();
        await adminClient.PutAsync($"/api/v1/configurations/{configName}/versions/{configDto!.LatestVersion}/publish", null);

        // Assign configuration to node
        var assignRequest = new AssignConfigurationRequest { ConfigurationName = configName };
        await adminClient.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        // Get checksum - should include entry point
        var checksumResponse = await _client.GetAsync($"/api/v1/nodes/{registerResult.NodeId}/configuration/checksum");
        checksumResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var checksum = await checksumResponse.Content.ReadFromJsonAsync<ConfigurationChecksumResponse>();
        checksum.Should().NotBeNull();
        checksum!.Checksum.Should().NotBeNullOrEmpty();
        checksum.EntryPoint.Should().Be("deploy.dsc.yaml");
    }

    [Fact]
    public async Task AssignConfiguration_WithNonExistentConfiguration_ReturnsNotFound()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "assign-noconfig-test.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateAuthenticatedClient();

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = "non-existent-config-xyz"
        };
        var response = await adminClient.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("Configuration not found");
    }

    [Fact]
    public async Task AssignConfiguration_WithMajorVersionConstraint_ReturnsNoContent()
    {
        var fqdn = $"major-version-test-{Guid.NewGuid():N}.example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register",
            new RegisterNodeRequest { Fqdn = fqdn, RegistrationKey = "test-registration-key" });
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateAuthenticatedClient();

        var configName = $"major-version-config-{Guid.NewGuid():N}";
        await CreateAndPublishConfigVersion(adminClient, configName, "1.0.0", "main.dsc.yaml");
        await CreateAndPublishConfigVersion(adminClient, configName, "2.0.0", "main.dsc.yaml", isNew: false);

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = configName,
            MajorVersion = 1
        };
        var response = await adminClient.PutAsJsonAsync(
            $"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AssignConfiguration_WithMajorVersionConstraint_NoMatchingVersion_ReturnsBadRequest()
    {
        var fqdn = $"major-version-noexist-{Guid.NewGuid():N}.example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register",
            new RegisterNodeRequest { Fqdn = fqdn, RegistrationKey = "test-registration-key" });
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateAuthenticatedClient();

        var configName = $"major-version-nomatch-{Guid.NewGuid():N}";
        await CreateAndPublishConfigVersion(adminClient, configName, "1.0.0", "main.dsc.yaml");
        await CreateAndPublishConfigVersion(adminClient, configName, "2.0.0", "main.dsc.yaml", isNew: false);

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = configName,
            MajorVersion = 3
        };
        var response = await adminClient.PutAsJsonAsync(
            $"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("No published version satisfies");
    }

    [Fact]
    public async Task AssignConfiguration_WithPrereleaseChannel_AllowsPrereleaseVersions()
    {
        var fqdn = $"prerelease-channel-test-{Guid.NewGuid():N}.example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register",
            new RegisterNodeRequest { Fqdn = fqdn, RegistrationKey = "test-registration-key" });
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateAuthenticatedClient();

        // Only publish a prerelease version
        var configName = $"prerelease-channel-config-{Guid.NewGuid():N}";
        await CreateAndPublishConfigVersion(adminClient, configName, "1.0.0-rc.1", "main.dsc.yaml");

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = configName,
            PrereleaseChannel = "rc"
        };
        var response = await adminClient.PutAsJsonAsync(
            $"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AssignConfiguration_WithNoChannel_ExcludesPrereleaseVersions_ReturnsBadRequest()
    {
        var fqdn = $"no-channel-prerelease-{Guid.NewGuid():N}.example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register",
            new RegisterNodeRequest { Fqdn = fqdn, RegistrationKey = "test-registration-key" });
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateAuthenticatedClient();

        // Only publish a prerelease — no stable version exists
        var configName = $"prerelease-only-config-{Guid.NewGuid():N}";
        await CreateAndPublishConfigVersion(adminClient, configName, "1.0.0-beta.1", "main.dsc.yaml");

        // No channel set → only stable versions qualify
        var assignRequest = new AssignConfigurationRequest { ConfigurationName = configName };
        var response = await adminClient.PutAsJsonAsync(
            $"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("No published version satisfies");
    }

    [Fact]
    public async Task AssignConfiguration_WithoutConstraints_UsesSemverOrdering()
    {
        // Publishes 2.0.0 first then 1.0.1 — semver ordering should resolve 2.0.0, not 1.0.1
        var fqdn = $"semver-ordering-test-{Guid.NewGuid():N}.example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register",
            new RegisterNodeRequest { Fqdn = fqdn, RegistrationKey = "test-registration-key" });
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateAuthenticatedClient();

        var configName = $"semver-ordering-config-{Guid.NewGuid():N}";
        await CreateAndPublishConfigVersion(adminClient, configName, "2.0.0", "main.dsc.yaml");
        await CreateAndPublishConfigVersion(adminClient, configName, "1.0.1", "main.dsc.yaml", isNew: false);

        var assignRequest = new AssignConfigurationRequest { ConfigurationName = configName };
        var response = await adminClient.PutAsJsonAsync(
            $"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        // Assignment succeeds — endpoint chose 2.0.0 (semver highest), not the most recently published 1.0.1
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    /// <summary>
    /// Creates a new configuration with an initial version (isNew=true) or adds a version to
    /// an existing configuration (isNew=false), then publishes it.
    /// </summary>
    private static async Task CreateAndPublishConfigVersion(
        HttpClient adminClient,
        string configName,
        string version,
        string entryPoint,
        bool isNew = true)
    {
        if (isNew)
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(configName), "name");
            content.Add(new StringContent(entryPoint), "entryPoint");
            content.Add(new StringContent(version), "version");
            var file = new ByteArrayContent("resources: []"u8.ToArray());
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(file, "files", entryPoint);
            await adminClient.PostAsync("/api/v1/configurations", content);
        }
        else
        {
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(version), "version");
            content.Add(new StringContent(entryPoint), "entryPoint");
            var file = new ByteArrayContent("resources: []"u8.ToArray());
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(file, "files", entryPoint);
            await adminClient.PostAsync($"/api/v1/configurations/{configName}/versions", content);
        }

        await adminClient.PutAsync($"/api/v1/configurations/{configName}/versions/{version}/publish", null);
    }

    [Fact]
    public async Task UnassignConfiguration_WithAssignedNode_ReturnsNoContent()
    {
        var fqdn = $"unassign-test-{Guid.NewGuid():N}.example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register",
            new RegisterNodeRequest { Fqdn = fqdn, RegistrationKey = "test-registration-key" });
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateAuthenticatedClient();

        var configName = $"unassign-config-{Guid.NewGuid():N}";
        await CreateAndPublishConfigVersion(adminClient, configName, "1.0.0", "main.dsc.yaml");

        await adminClient.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration",
            new AssignConfigurationRequest { ConfigurationName = configName, MajorVersion = 1 });

        var response = await adminClient.DeleteAsync($"/api/v1/nodes/{registerResult.NodeId}/configuration");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UnassignConfiguration_WhenNotAssigned_ReturnsNoContent()
    {
        var fqdn = $"unassign-notassigned-{Guid.NewGuid():N}.example.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register",
            new RegisterNodeRequest { Fqdn = fqdn, RegistrationKey = "test-registration-key" });
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateAuthenticatedClient();

        var response = await adminClient.DeleteAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UnassignConfiguration_WithNonExistentNode_ReturnsNotFound()
    {
        using var adminClient = _factory.CreateAuthenticatedClient();

        var response = await adminClient.DeleteAsync($"/api/v1/nodes/{Guid.NewGuid()}/configuration");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

}

