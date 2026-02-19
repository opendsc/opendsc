// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Headers;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Contracts;
using OpenDsc.Server.Data;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class ParameterEndpointsTests : IDisposable
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

    private async Task<Guid> CreateTestConfigurationAsync(HttpClient client, string name)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "name");
        content.Add(new StringContent("main.dsc.yaml"), "entryPoint");
        var file = new ByteArrayContent("resources: []"u8.ToArray());
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(file, "files", "main.dsc.yaml");

        var response = await client.PostAsync("/api/v1/configurations", content);
        response.EnsureSuccessStatusCode();

        // Get the configuration ID from the database
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
        var config = await db.Configurations.FirstOrDefaultAsync(c => c.Name == name);
        if (config is null)
        {
            throw new InvalidOperationException($"Configuration '{name}' was not found after creation");
        }
        return config.Id;
    }

    [Fact]
    public async Task CreateOrUpdateParameter_WithValidData_CreatesNewParameter()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var scopeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Default scope type

        var request = new
        {
            version = "1.0.0",
            content = "param1: value1\nparam2: value2",
            contentType = "application/x-yaml",
            isDraft = false
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParameterFileDto>();
        result.Should().NotBeNull();
        result!.Version.Should().Be("1.0.0");
        result.IsDraft.Should().BeFalse();
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task GetParameterVersions_WithValidData_ReturnsVersions()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var scopeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Default scope type

        // Create a parameter version
        var createRequest = new
        {
            version = "1.0.0",
            content = "param1: value1",
            contentType = "application/x-yaml",
            isDraft = false
        };

        await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", createRequest);

        // Act
        var response = await client.GetAsync($"/api/v1/parameters/{scopeTypeId}/{configId}/versions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await response.Content.ReadFromJsonAsync<List<ParameterFileDto>>();
        versions.Should().NotBeNull();
        versions.Should().HaveCount(1);
        versions![0].Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task ActivateParameterVersion_WithValidData_ActivatesVersion()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var scopeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Default scope type

        // Create a parameter version
        var createRequest = new
        {
            version = "1.0.0",
            content = "param1: value1",
            contentType = "application/x-yaml",
            isDraft = false
        };

        await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", createRequest);

        // Act
        var response = await client.PutAsync($"/api/v1/parameters/{scopeTypeId}/{configId}/versions/1.0.0/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParameterFileDto>();
        result.Should().NotBeNull();
        result!.Version.Should().Be("1.0.0");
        result.IsActive.Should().BeTrue();
        result.IsDraft.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteParameterVersion_WithInactiveVersion_DeletesVersion()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var scopeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Default scope type

        // Create a parameter version
        var createRequest = new
        {
            version = "1.0.0",
            content = "param1: value1",
            contentType = "application/x-yaml",
            isDraft = false
        };

        await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", createRequest);

        // Act
        var response = await client.DeleteAsync($"/api/v1/parameters/{scopeTypeId}/{configId}/versions/1.0.0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteParameterVersion_WithActiveVersion_ReturnsConflict()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var scopeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // Default scope type

        // Create and activate a parameter version
        var createRequest = new
        {
            version = "1.0.0",
            content = "param1: value1",
            contentType = "application/x-yaml",
            isDraft = false
        };

        await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", createRequest);
        await client.PutAsync($"/api/v1/parameters/{scopeTypeId}/{configId}/versions/1.0.0/activate", null);

        // Act
        var response = await client.DeleteAsync($"/api/v1/parameters/{scopeTypeId}/{configId}/versions/1.0.0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetNodeParameterProvenance_WithValidNode_ReturnsProvenance()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var configName = $"test-config-{Guid.NewGuid()}";
        var configId = await CreateTestConfigurationAsync(client, configName);

        // Register a node
        var registerRequest = new RegisterNodeRequest { Fqdn = "test-node.example.com", RegistrationKey = "test-lcm-registration-key" };
        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);
        if (!registerResponse.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Node registration failed: {registerResponse.StatusCode} - {errorContent}");
        }
        var registration = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();
        var nodeId = registration!.NodeId;

        // Assign configuration to node
        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = configName
        };

        var assignResponse = await client.PutAsJsonAsync($"/api/v1/nodes/{nodeId}/configuration", assignRequest);
        if (!assignResponse.IsSuccessStatusCode)
        {
            var errorContent = await assignResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Configuration assignment failed: {assignResponse.StatusCode} - {errorContent}");
        }

        // Act
        var response = await client.GetAsync($"/api/v1/nodes/{nodeId}/parameters/provenance");
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Provenance request failed: {response.StatusCode} - {errorContent}");
        }
        var result = await response.Content.ReadFromJsonAsync<ParameterProvenanceDto>();
        result.Should().NotBeNull();
        result!.NodeId.Should().Be(nodeId);
        result.ConfigurationId.Should().Be(configId);
    }
}

public sealed class ParameterFileDto
{
    public required Guid Id { get; init; }
    public required Guid ScopeTypeId { get; init; }
    public required Guid ConfigurationId { get; init; }
    public string? ScopeValue { get; init; }
    public required string Version { get; init; }
    public required string Checksum { get; init; }
    public required bool IsActive { get; init; }
    public required bool IsDraft { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class ConfigurationSummaryDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string EntryPoint { get; init; }
    public required bool IsServerManaged { get; init; }
    public required int VersionCount { get; init; }
    public string? LatestVersion { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class NodeDto
{
    public required Guid Id { get; init; }
    public required string Fqdn { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

public sealed class RegisterNodeRequest
{
    public required string RegistrationKey { get; set; }
    public required string Fqdn { get; set; }
}

public sealed class RegisterNodeResponse
{
    public required Guid NodeId { get; set; }
}

public sealed class ParameterProvenanceDto
{
    public required Guid NodeId { get; init; }
    public required Guid ConfigurationId { get; init; }
    public required string MergedParameters { get; init; }
    public required Dictionary<string, ParameterSourceInfo> Provenance { get; init; }
}

public sealed class ParameterSourceInfo
{
    public required string ScopeTypeName { get; init; }
    public string? ScopeValue { get; init; }
    public required int Precedence { get; init; }
    public required object? Value { get; init; }
    public List<ScopeInfo>? OverriddenBy { get; init; }
}

public sealed class ScopeInfo
{
    public required string ScopeTypeName { get; init; }
    public string? ScopeValue { get; init; }
    public required int Precedence { get; init; }
    public required object? Value { get; init; }
}
