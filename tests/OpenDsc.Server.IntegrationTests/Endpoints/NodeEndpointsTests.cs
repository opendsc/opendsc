// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Json;

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
}
