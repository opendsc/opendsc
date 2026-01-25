// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using FluentAssertions;

using OpenDsc.Server.Contracts;

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
    public async Task RegisterNode_WithValidRegistrationKey_ReturnsNodeIdAndApiKey()
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
        result.ApiKey.Should().NotBeNullOrEmpty();
        result.KeyRotationInterval.Should().BeGreaterThan(TimeSpan.Zero);
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
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().Contain("FQDN is required");
    }

    [Fact]
    public async Task RegisterNode_ReRegistrationOfExistingNode_ReturnsNewApiKey()
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
        secondResult.ApiKey.Should().NotBe(firstResult.ApiKey);
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

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

        var response = await client.GetAsync("/api/v1/nodes/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var nodes = await response.Content.ReadFromJsonAsync<List<NodeSummary>>();
        nodes.Should().NotBeNull();
        nodes!.Should().NotBeEmpty();
        nodes.Should().Contain(n => n.Fqdn == "list-test.example.com");
    }

    [Fact]
    public async Task GetConfigurationChecksum_WithoutConfiguration_ReturnsNotFound()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "checksum-test.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerResult!.ApiKey);

        var response = await client.GetAsync($"/api/v1/nodes/{registerResult.NodeId}/configuration/checksum");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

        var response = await client.GetAsync($"/api/v1/nodes/{registerResult!.NodeId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var node = await response.Content.ReadFromJsonAsync<NodeSummary>();
        node.Should().NotBeNull();
        node!.Id.Should().Be(registerResult.NodeId);
        node.Fqdn.Should().Be("getnode-test.example.com");
    }

    [Fact]
    public async Task GetNode_WithNonExistentNode_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

        var response = await client.GetAsync($"/api/v1/nodes/{Guid.NewGuid()}");

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

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

        var response = await client.DeleteAsync($"/api/v1/nodes/{registerResult!.NodeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync($"/api/v1/nodes/{registerResult.NodeId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteNode_WithNonExistentNode_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

        var response = await client.DeleteAsync($"/api/v1/nodes/{Guid.NewGuid()}");

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

        using var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

        var createConfigRequest = new CreateConfigurationRequest
        {
            Name = "test-assign-config",
            Content = "# Test configuration\n"
        };
        await adminClient.PostAsJsonAsync("/api/v1/configurations", createConfigRequest);

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

        using var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = ""
        };
        var response = await adminClient.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Contain("Configuration name is required");
    }

    [Fact]
    public async Task AssignConfiguration_WithNonExistentNode_ReturnsNotFound()
    {
        using var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

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
    public async Task AssignConfiguration_WithNonExistentConfiguration_ReturnsNotFound()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "assign-noconfig-test.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

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
    public async Task GetNodeConfiguration_WithAssignedConfiguration_ReturnsContent()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "getconfig-test.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

        var createConfigRequest = new CreateConfigurationRequest
        {
            Name = "test-getconfig-config",
            Content = "# Test get configuration content\n"
        };
        await adminClient.PostAsJsonAsync("/api/v1/configurations", createConfigRequest);

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = "test-getconfig-config"
        };
        await adminClient.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        using var nodeClient = _factory.CreateClient();
        nodeClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerResult.ApiKey);

        var response = await nodeClient.GetAsync($"/api/v1/nodes/{registerResult.NodeId}/configuration");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Test get configuration content");
    }

    [Fact]
    public async Task GetNodeConfiguration_WithWrongNodeId_ReturnsForbidden()
    {
        var registerRequest1 = new RegisterNodeRequest
        {
            Fqdn = "getconfig-forbidden1.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse1 = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest1);
        var registerResult1 = await registerResponse1.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        var registerRequest2 = new RegisterNodeRequest
        {
            Fqdn = "getconfig-forbidden2.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse2 = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest2);
        var registerResult2 = await registerResponse2.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerResult1!.ApiKey);

        var response = await client.GetAsync($"/api/v1/nodes/{registerResult2!.NodeId}/configuration");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetNodeConfiguration_WithNonExistentNode_ReturnsUnauthorized()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "getconfig-notfound.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");
        await adminClient.DeleteAsync($"/api/v1/nodes/{registerResult!.NodeId}");

        using var nodeClient = _factory.CreateClient();
        nodeClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerResult.ApiKey);

        var response = await nodeClient.GetAsync($"/api/v1/nodes/{registerResult.NodeId}/configuration");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RotateKey_WithValidNodeAuth_ReturnsNewApiKey()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "rotate-test.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerResult!.ApiKey);

        var response = await client.PostAsync($"/api/v1/nodes/{registerResult.NodeId}/rotate-key", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RotateKeyResponse>();
        result.Should().NotBeNull();
        result!.ApiKey.Should().NotBe(registerResult.ApiKey);
        result.ApiKey.Should().NotBeNullOrEmpty();
        result.KeyRotationInterval.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task RotateKey_WithWrongNodeId_ReturnsForbidden()
    {
        var registerRequest1 = new RegisterNodeRequest
        {
            Fqdn = "rotate-forbidden1.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse1 = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest1);
        var registerResult1 = await registerResponse1.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        var registerRequest2 = new RegisterNodeRequest
        {
            Fqdn = "rotate-forbidden2.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse2 = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest2);
        var registerResult2 = await registerResponse2.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerResult1!.ApiKey);

        var response = await client.PostAsync($"/api/v1/nodes/{registerResult2!.NodeId}/rotate-key", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RotateKey_WithNonExistentNode_ReturnsUnauthorized()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "rotate-notfound.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");
        await adminClient.DeleteAsync($"/api/v1/nodes/{registerResult!.NodeId}");

        using var nodeClient = _factory.CreateClient();
        nodeClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerResult.ApiKey);

        var response = await nodeClient.PostAsync($"/api/v1/nodes/{registerResult.NodeId}/rotate-key", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConfigurationChecksum_WithAssignedConfiguration_ReturnsChecksum()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = "checksum-assigned-test.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var adminClient = _factory.CreateClient();
        adminClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "test-admin-key");

        var createConfigRequest = new CreateConfigurationRequest
        {
            Name = "test-checksum-config",
            Content = "# Test checksum configuration\n"
        };
        await adminClient.PostAsJsonAsync("/api/v1/configurations", createConfigRequest);

        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = "test-checksum-config"
        };
        await adminClient.PutAsJsonAsync($"/api/v1/nodes/{registerResult!.NodeId}/configuration", assignRequest);

        using var nodeClient = _factory.CreateClient();
        nodeClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerResult.ApiKey);

        var response = await nodeClient.GetAsync($"/api/v1/nodes/{registerResult.NodeId}/configuration/checksum");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ConfigurationChecksumResponse>();
        result.Should().NotBeNull();
        result!.Checksum.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetConfigurationChecksum_WithWrongNodeId_ReturnsForbidden()
    {
        var registerRequest1 = new RegisterNodeRequest
        {
            Fqdn = "checksum-forbidden1.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse1 = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest1);
        var registerResult1 = await registerResponse1.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        var registerRequest2 = new RegisterNodeRequest
        {
            Fqdn = "checksum-forbidden2.example.com",
            RegistrationKey = "test-registration-key"
        };
        var registerResponse2 = await _client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest2);
        var registerResult2 = await registerResponse2.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registerResult1!.ApiKey);

        var response = await client.GetAsync($"/api/v1/nodes/{registerResult2!.NodeId}/configuration/checksum");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
