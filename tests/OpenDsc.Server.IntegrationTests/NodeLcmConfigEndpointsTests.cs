// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

using AwesomeAssertions;

using OpenDsc.Lcm.Contracts;
using OpenDsc.Server.Contracts;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class NodeLcmConfigEndpointsTests : IDisposable
{
    private readonly ServerWebApplicationFactory _factory = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public void Dispose()
    {
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task<Guid> RegisterTestNodeAsync()
    {
        using var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", new RegisterNodeRequest
        {
            RegistrationKey = "test-registration-key",
            Fqdn = $"test-node-{Guid.NewGuid()}.example.com"
        });

        registerResponse.EnsureSuccessStatusCode();
        var registration = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();
        return registration!.NodeId;
    }

    [Fact]
    public async Task GetLcmConfig_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var nodeId = Guid.NewGuid();
        var response = await client.GetAsync($"/api/v1/nodes/{nodeId}/lcm-config");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLcmConfig_WithNodeAuth_ReturnsOkWithNullsForNewNode()
    {
        var nodeId = await RegisterTestNodeAsync();

        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/nodes/{nodeId}/lcm-config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<NodeLcmConfigResponse>(JsonOptions);
        config.Should().NotBeNull();
        config!.ConfigurationMode.Should().BeNull();
        config.ConfigurationModeInterval.Should().BeNull();
        config.ReportCompliance.Should().BeNull();
    }

    [Fact]
    public async Task GetLcmConfig_WithNodeAuth_ReturnsConfiguredValues()
    {
        var nodeId = await RegisterTestNodeAsync();

        // Set config via admin endpoint first
        using var adminClient = _factory.CreateAuthenticatedClient();
        var updateRequest = new UpdateNodeLcmConfigRequest
        {
            ConfigurationMode = ConfigurationMode.Remediate,
            ConfigurationModeInterval = TimeSpan.FromMinutes(10),
            ReportCompliance = true
        };
        var updateResponse = await adminClient.PutAsJsonAsync($"/api/v1/nodes/{nodeId}/lcm-config", updateRequest);
        updateResponse.EnsureSuccessStatusCode();

        // Fetch via node endpoint
        using var nodeClient = _factory.CreateClient();
        var response = await nodeClient.GetAsync($"/api/v1/nodes/{nodeId}/lcm-config");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<NodeLcmConfigResponse>(JsonOptions);
        config.Should().NotBeNull();
        config!.ConfigurationMode.Should().Be(ConfigurationMode.Remediate);
        config.ConfigurationModeInterval.Should().Be(TimeSpan.FromMinutes(10));
        config.ReportCompliance.Should().BeTrue();
    }

    [Fact]
    public async Task GetLcmConfig_WithNonExistentNode_ReturnsForbiddenOrNotFound()
    {
        var fakeId = Guid.NewGuid();
        using var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/v1/nodes/{fakeId}/lcm-config");
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    // ── PUT /{nodeId}/lcm-config ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateLcmConfig_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var nodeId = Guid.NewGuid();
        var response = await client.PutAsJsonAsync($"/api/v1/nodes/{nodeId}/lcm-config", new UpdateNodeLcmConfigRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateLcmConfig_WithAdminAuth_ReturnsOk()
    {
        var nodeId = await RegisterTestNodeAsync();

        using var adminClient = _factory.CreateAuthenticatedClient();
        var request = new UpdateNodeLcmConfigRequest
        {
            ConfigurationMode = ConfigurationMode.Remediate,
            ConfigurationModeInterval = TimeSpan.FromMinutes(30),
            ReportCompliance = false
        };

        var response = await adminClient.PutAsJsonAsync($"/api/v1/nodes/{nodeId}/lcm-config", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<NodeLcmConfigResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.ConfigurationMode.Should().Be(ConfigurationMode.Remediate);
        result.ConfigurationModeInterval.Should().Be(TimeSpan.FromMinutes(30));
        result.ReportCompliance.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateLcmConfig_WithAdminAuth_PersistsChanges()
    {
        var nodeId = await RegisterTestNodeAsync();

        using var adminClient = _factory.CreateAuthenticatedClient();
        var request = new UpdateNodeLcmConfigRequest
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationModeInterval = TimeSpan.FromMinutes(5),
            ReportCompliance = true
        };

        await adminClient.PutAsJsonAsync($"/api/v1/nodes/{nodeId}/lcm-config", request);

        // Verify persisted via node summary
        var nodeResponse = await adminClient.GetAsync($"/api/v1/nodes/{nodeId}");
        nodeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var node = await nodeResponse.Content.ReadFromJsonAsync<NodeSummary>(JsonOptions);
        node.Should().NotBeNull();
        node!.DesiredConfigurationMode.Should().Be(ConfigurationMode.Monitor);
        node.DesiredConfigurationModeInterval.Should().Be(TimeSpan.FromMinutes(5));
        node.DesiredReportCompliance.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateLcmConfig_WithAdminAuth_ClearsValuesWhenNull()
    {
        var nodeId = await RegisterTestNodeAsync();

        using var adminClient = _factory.CreateAuthenticatedClient();

        // First set some values
        await adminClient.PutAsJsonAsync($"/api/v1/nodes/{nodeId}/lcm-config", new UpdateNodeLcmConfigRequest
        {
            ConfigurationMode = ConfigurationMode.Remediate,
            ConfigurationModeInterval = TimeSpan.FromMinutes(20),
            ReportCompliance = false
        });

        // Then clear them
        var clearResponse = await adminClient.PutAsJsonAsync($"/api/v1/nodes/{nodeId}/lcm-config", new UpdateNodeLcmConfigRequest
        {
            ConfigurationMode = null,
            ConfigurationModeInterval = null,
            ReportCompliance = null
        });

        clearResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await clearResponse.Content.ReadFromJsonAsync<NodeLcmConfigResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.ConfigurationMode.Should().BeNull();
        result.ConfigurationModeInterval.Should().BeNull();
        result.ReportCompliance.Should().BeNull();
    }

    [Fact]
    public async Task UpdateLcmConfig_WithAdminAuth_NonExistentNode_ReturnsNotFound()
    {
        var fakeId = Guid.NewGuid();
        using var adminClient = _factory.CreateAuthenticatedClient();
        var response = await adminClient.PutAsJsonAsync($"/api/v1/nodes/{fakeId}/lcm-config", new UpdateNodeLcmConfigRequest());
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /{nodeId}/reported-config ──────────────────────────────────────────

    [Fact]
    public async Task ReportConfig_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var nodeId = Guid.NewGuid();
        var response = await client.PutAsJsonAsync($"/api/v1/nodes/{nodeId}/reported-config", new ReportNodeLcmConfigRequest());
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReportConfig_WithNodeAuth_ReturnsNoContent()
    {
        var nodeId = await RegisterTestNodeAsync();

        using var client = _factory.CreateClient();
        var request = new ReportNodeLcmConfigRequest
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ConfigurationModeInterval = TimeSpan.FromMinutes(15),
            ReportCompliance = true
        };

        var response = await client.PutAsJsonAsync($"/api/v1/nodes/{nodeId}/reported-config", request);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReportConfig_WithNodeAuth_UpdatesReportedFields()
    {
        var nodeId = await RegisterTestNodeAsync();

        using var nodeClient = _factory.CreateClient();
        var request = new ReportNodeLcmConfigRequest
        {
            ConfigurationMode = ConfigurationMode.Remediate,
            ConfigurationModeInterval = TimeSpan.FromMinutes(20),
            ReportCompliance = false
        };

        await nodeClient.PutAsJsonAsync($"/api/v1/nodes/{nodeId}/reported-config", request);

        using var adminClient = _factory.CreateAuthenticatedClient();
        var nodeResponse = await adminClient.GetAsync($"/api/v1/nodes/{nodeId}");
        nodeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var node = await nodeResponse.Content.ReadFromJsonAsync<NodeSummary>(JsonOptions);
        node.Should().NotBeNull();
        node!.ConfigurationMode.Should().Be(ConfigurationMode.Remediate);
        node.ConfigurationModeInterval.Should().Be(TimeSpan.FromMinutes(20));
        node.ReportCompliance.Should().BeFalse();
    }

    [Fact]
    public async Task ReportConfig_DoesNotOverwriteDesiredValues()
    {
        var nodeId = await RegisterTestNodeAsync();

        // Admin sets desired overrides
        using var adminClient = _factory.CreateAuthenticatedClient();
        await adminClient.PutAsJsonAsync($"/api/v1/nodes/{nodeId}/lcm-config", new UpdateNodeLcmConfigRequest
        {
            ConfigurationMode = ConfigurationMode.Remediate,
            ReportCompliance = true
        });

        // Node reports different current values
        using var nodeClient = _factory.CreateClient();
        await nodeClient.PutAsJsonAsync($"/api/v1/nodes/{nodeId}/reported-config", new ReportNodeLcmConfigRequest
        {
            ConfigurationMode = ConfigurationMode.Monitor,
            ReportCompliance = false
        });

        // Desired values should be unchanged
        var lcmConfigResponse = await nodeClient.GetAsync($"/api/v1/nodes/{nodeId}/lcm-config");
        var desired = await lcmConfigResponse.Content.ReadFromJsonAsync<NodeLcmConfigResponse>(JsonOptions);
        desired.Should().NotBeNull();
        desired!.ConfigurationMode.Should().Be(ConfigurationMode.Remediate);
        desired.ReportCompliance.Should().BeTrue();

        // Reported values should reflect what the node sent
        var nodeResponse = await adminClient.GetAsync($"/api/v1/nodes/{nodeId}");
        var node = await nodeResponse.Content.ReadFromJsonAsync<NodeSummary>(JsonOptions);
        node!.ConfigurationMode.Should().Be(ConfigurationMode.Monitor);
        node.ReportCompliance.Should().BeFalse();
    }

    [Fact]
    public async Task ReportConfig_WithNodeAuth_NonExistentNode_ReturnsNotFound()
    {
        var fakeId = Guid.NewGuid();
        using var client = _factory.CreateClient();
        var response = await client.PutAsJsonAsync($"/api/v1/nodes/{fakeId}/reported-config", new ReportNodeLcmConfigRequest());
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }
}
