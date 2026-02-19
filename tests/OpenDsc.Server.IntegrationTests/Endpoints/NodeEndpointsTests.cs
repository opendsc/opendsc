// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

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

}

