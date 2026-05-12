// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Headers;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Contracts.Nodes;
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

        // Upload a parameter schema with common parameters so tests can create parameter files
        // Use supported parameter types: string, secureString, int, bool, object, secureObject, array, float, double
        var schemaContent = @"{
  ""parameters"": {
    ""param1"": { ""type"": ""string"" },
    ""param2"": { ""type"": ""string"" },
    ""setting1"": { ""type"": ""string"" },
    ""appName"": { ""type"": ""string"" },
    ""port"": { ""type"": ""int"", ""minValue"": 1, ""maxValue"": 65535 }
  }
}";
        using var schemaRequest = new MultipartFormDataContent();
        schemaRequest.Add(new StringContent("1.0.0"), "version");
        var schemaFile = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(schemaContent));
        schemaFile.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        schemaRequest.Add(schemaFile, "parametersFile", "parameters.json");

        var schemaResponse = await client.PutAsync($"/api/v1/configurations/{name}/parameters", schemaRequest);
        if (!schemaResponse.IsSuccessStatusCode)
        {
            var errorContent = await schemaResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Parameter schema upload failed: {schemaResponse.StatusCode} - {errorContent}");
        }

        // Verify schema was created
        await Task.Delay(100); // Small delay to ensure data is persisted
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ServerDbContext>();
        var schema = await verifyDb.ParameterSchemas.FirstOrDefaultAsync(
            ps => ps.ConfigurationId == config.Id && ps.SchemaVersion == "1.0.0");
        if (schema is null)
        {
            throw new InvalidOperationException($"Parameter schema was not created for configuration '{name}'");
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
        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParameterFileDto>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Version.Should().Be("1.0.0");
        result.Status.Should().Be("Draft");
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

        await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", createRequest, TestContext.Current.CancellationToken);

        // Act
        var response = await client.GetAsync($"/api/v1/parameters/{scopeTypeId}/{configId}/versions", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await response.Content.ReadFromJsonAsync<List<ParameterFileDto>>(TestContext.Current.CancellationToken);
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

        await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", createRequest, TestContext.Current.CancellationToken);

        // Act
        var response = await client.PutAsync($"/api/v1/parameters/{scopeTypeId}/{configId}/versions/1.0.0/publish", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParameterFileDto>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Version.Should().Be("1.0.0");
        result.Status.Should().Be("Published");
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

        await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", createRequest, TestContext.Current.CancellationToken);

        // Act
        var response = await client.DeleteAsync($"/api/v1/parameters/{scopeTypeId}/{configId}/versions/1.0.0", TestContext.Current.CancellationToken);

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

        await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", createRequest, TestContext.Current.CancellationToken);
        await client.PutAsync($"/api/v1/parameters/{scopeTypeId}/{configId}/versions/1.0.0/publish", null, TestContext.Current.CancellationToken);

        // Act
        var response = await client.DeleteAsync($"/api/v1/parameters/{scopeTypeId}/{configId}/versions/1.0.0", TestContext.Current.CancellationToken);

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

        // Create and publish a configuration version so assignment succeeds
        using var versionContent = new MultipartFormDataContent();
        versionContent.Add(new StringContent("1.0.0"), "version");
        var versionFile = new ByteArrayContent("resources: []"u8.ToArray());
        versionFile.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        versionContent.Add(versionFile, "files", "main.dsc.yaml");
        await client.PostAsync($"/api/v1/configurations/{configName}/versions", versionContent, TestContext.Current.CancellationToken);
        await client.PutAsync($"/api/v1/configurations/{configName}/versions/1.0.0/publish", null, TestContext.Current.CancellationToken);

        // Register a node
        var registerRequest = new RegisterNodeRequest { Fqdn = "test-node.example.com", RegistrationKey = "test-lcm-registration-key" };
        var registerResponse = await client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest, TestContext.Current.CancellationToken);
        if (!registerResponse.IsSuccessStatusCode)
        {
            var errorContent = await registerResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            throw new InvalidOperationException($"Node registration failed: {registerResponse.StatusCode} - {errorContent}");
        }
        var registration = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>(TestContext.Current.CancellationToken);
        var nodeId = registration!.NodeId;

        // Assign configuration to node
        var assignRequest = new AssignConfigurationRequest
        {
            ConfigurationName = configName
        };

        var assignResponse = await client.PutAsJsonAsync($"/api/v1/nodes/{nodeId}/configuration", assignRequest, TestContext.Current.CancellationToken);
        if (!assignResponse.IsSuccessStatusCode)
        {
            var errorContent = await assignResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            throw new InvalidOperationException($"Configuration assignment failed: {assignResponse.StatusCode} - {errorContent}");
        }

        // Act
        var response = await client.GetAsync($"/api/v1/nodes/{nodeId}/parameters/provenance", TestContext.Current.CancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            throw new InvalidOperationException($"Provenance request failed: {response.StatusCode} - {errorContent}");
        }
        var result = await response.Content.ReadFromJsonAsync<ParameterProvenanceDto>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.NodeId.Should().Be(nodeId);
        result.ConfigurationId.Should().Be(configId);
    }

    [Fact]
    public async Task ValidateParameterFile_WithValidParameters_ReturnsSuccess()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var configName = $"validate-test-{Guid.NewGuid()}";
        var configId = await CreateTestConfigurationAsync(client, configName);

        // Create parameter schema
        var schemaContent = @"{
  ""parameters"": {
    ""appName"": { ""type"": ""string"" },
    ""port"": { ""type"": ""int"", ""minValue"": 1, ""maxValue"": 65535 }
  }
}";

        using var schemaRequest = new MultipartFormDataContent();
        schemaRequest.Add(new StringContent("1.0.0"), "version");
        var schemaFile = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(schemaContent));
        schemaFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        schemaRequest.Add(schemaFile, "parametersFile", "parameters.json");

        await client.PutAsync($"/api/v1/configurations/{configName}/parameters", schemaRequest, TestContext.Current.CancellationToken);

        // Act - Validate a parameter file
        var paramContent = @"{
  ""parameters"": {
    ""appName"": ""MyApp"",
    ""port"": 8080
  }
}";

        var validateResponse = await client.PostAsync(
            $"/api/v1/configurations/{configName}/parameters/validate?version=1.0.0",
            new StringContent(paramContent, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        // Assert
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await validateResponse.Content.ReadFromJsonAsync<ValidationResultDto>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.IsValid.Should().BeTrue();
        result.Errors.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateParameterFile_WithInvalidParameters_ReturnsErrors()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var configName = $"validate-test-{Guid.NewGuid()}";
        await CreateTestConfigurationAsync(client, configName);

        // Create parameter schema
        var schemaContent = @"{
  ""parameters"": {
    ""appName"": { ""type"": ""string"" },
    ""port"": { ""type"": ""int"", ""minValue"": 1, ""maxValue"": 65535 }
  }
}";

        using var schemaRequest = new MultipartFormDataContent();
        schemaRequest.Add(new StringContent("1.0.0"), "version");
        var schemaFile = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(schemaContent));
        schemaFile.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        schemaRequest.Add(schemaFile, "parametersFile", "parameters.json");

        await client.PutAsync($"/api/v1/configurations/{configName}/parameters", schemaRequest, TestContext.Current.CancellationToken);

        // Act - Validate with invalid port value
        var paramContent = @"{
  ""parameters"": {
    ""appName"": ""MyApp"",
    ""port"": 99999
  }
}";

        var validateResponse = await client.PostAsync(
            $"/api/v1/configurations/{configName}/parameters/validate?version=1.0.0",
            new StringContent(paramContent, System.Text.Encoding.UTF8, "application/json"), TestContext.Current.CancellationToken);

        // Assert
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await validateResponse.Content.ReadFromJsonAsync<ValidationResultDto>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeNullOrEmpty();
        result.Errors.Should().Contain(e => e.Path.Contains("port"));
    }

    private async Task<(Guid ScopeTypeId, Guid ScopeValueId)> CreateRestrictedScopeTypeWithValueAsync(
        HttpClient client, string scopeTypeName, string scopeValue)
    {
        // Create restricted scope type
        var scopeTypeRequest = new { name = scopeTypeName, valueMode = "Restricted" };
        var scopeTypeResponse = await client.PostAsJsonAsync("/api/v1/scope-types", scopeTypeRequest);
        scopeTypeResponse.EnsureSuccessStatusCode();
        var ScopeTypeDetails = await scopeTypeResponse.Content.ReadFromJsonAsync<ScopeTypeSimpleDto>();
        var scopeTypeId = ScopeTypeDetails!.Id;

        // Create scope value
        var scopeValueRequest = new { value = scopeValue };
        var scopeValueResponse = await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeTypeId}/values", scopeValueRequest);
        scopeValueResponse.EnsureSuccessStatusCode();
        var ScopeValueDetails = await scopeValueResponse.Content.ReadFromJsonAsync<ScopeValueSimpleDto>();
        var scopeValueId = ScopeValueDetails!.Id;

        return (scopeTypeId, scopeValueId);
    }

    [Fact]
    public async Task CreateOrUpdateParameter_WithRestrictedScopeType_AndValidScopeValue_CreatesParameter()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var (scopeTypeId, _) = await CreateRestrictedScopeTypeWithValueAsync(client, $"Environment-{Guid.NewGuid()}", "Development");

        var request = new
        {
            scopeValue = "Development",
            version = "1.0.0",
            content = "parameters:\n  setting1: value1\n",
            contentType = "application/x-yaml",
            isDraft = true
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParameterFileDto>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.ScopeValue.Should().Be("Development");
        result.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task CreateOrUpdateParameter_WithRestrictedScopeType_AndNoScopeValue_ReturnsBadRequest()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var (scopeTypeId, _) = await CreateRestrictedScopeTypeWithValueAsync(client, $"Environment-{Guid.NewGuid()}", "Development");

        var request = new
        {
            // scopeValue intentionally omitted — scope type is Restricted so this should fail
            version = "1.0.0",
            content = "parameters:\n  setting1: value1\n",
            contentType = "application/x-yaml",
            isDraft = true
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrUpdateParameter_WithRestrictedScopeType_AndInvalidScopeValue_ReturnsBadRequest()
    {
        // Arrange
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var (scopeTypeId, _) = await CreateRestrictedScopeTypeWithValueAsync(client, $"Environment-{Guid.NewGuid()}", "Development");

        var request = new
        {
            scopeValue = "NonExistentValue",
            version = "1.0.0",
            content = "parameters:\n  setting1: value1\n",
            contentType = "application/x-yaml",
            isDraft = true
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Node scope type ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateOrUpdateParameter_WithNodeScopeType_AndNoScopeValue_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var nodeScopeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var request = new
        {
            // scopeValue intentionally omitted
            version = "1.0.0",
            content = "parameters:\n  setting1: value1\n",
            contentType = "application/x-yaml",
            isDraft = true
        };

        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{nodeScopeTypeId}/{configId}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrUpdateParameter_WithNodeScopeType_AndUnregisteredNode_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var nodeScopeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000002");

        var request = new
        {
            scopeValue = "not-registered.example.com",
            version = "1.0.0",
            content = "parameters:\n  setting1: value1\n",
            contentType = "application/x-yaml",
            isDraft = true
        };

        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{nodeScopeTypeId}/{configId}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Default scope type ───────────────────────────────────────────────────

    [Fact]
    public async Task CreateOrUpdateParameter_WithDefaultScopeType_AndScopeValueProvided_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var defaultScopeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var request = new
        {
            scopeValue = "should-not-be-allowed",
            version = "1.0.0",
            content = "parameters:\n  setting1: value1\n",
            contentType = "application/x-yaml",
            isDraft = true
        };

        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{defaultScopeTypeId}/{configId}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrUpdateParameter_WithDefaultScopeType_AndNoScopeValue_Succeeds()
    {
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var defaultScopeTypeId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        var request = new
        {
            version = "1.0.0",
            content = "parameters:\n  setting1: value1\n",
            contentType = "application/x-yaml",
            isDraft = true
        };

        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{defaultScopeTypeId}/{configId}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParameterFileDto>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.ScopeValue.Should().BeNullOrEmpty();
        result.Status.Should().Be("Draft");
    }

    // ── Unrestricted (user-created) scope type ───────────────────────────────

    [Fact]
    public async Task CreateOrUpdateParameter_WithUnrestrictedScopeType_AndNoScopeValue_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var scopeTypeRequest = new { name = $"Region-{Guid.NewGuid()}", valueMode = "Unrestricted" };
        var scopeTypeResponse = await client.PostAsJsonAsync("/api/v1/scope-types", scopeTypeRequest, TestContext.Current.CancellationToken);
        scopeTypeResponse.EnsureSuccessStatusCode();
        var ScopeTypeDetails = await scopeTypeResponse.Content.ReadFromJsonAsync<ScopeTypeSimpleDto>(TestContext.Current.CancellationToken);

        var request = new
        {
            // scopeValue intentionally omitted
            version = "1.0.0",
            content = "parameters:\n  setting1: value1\n",
            contentType = "application/x-yaml",
            isDraft = true
        };

        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{ScopeTypeDetails!.Id}/{configId}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrUpdateParameter_WithUnrestrictedScopeType_AndScopeValue_Succeeds()
    {
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var scopeTypeRequest = new { name = $"Region-{Guid.NewGuid()}", valueMode = "Unrestricted" };
        var scopeTypeResponse = await client.PostAsJsonAsync("/api/v1/scope-types", scopeTypeRequest, TestContext.Current.CancellationToken);
        scopeTypeResponse.EnsureSuccessStatusCode();
        var ScopeTypeDetails = await scopeTypeResponse.Content.ReadFromJsonAsync<ScopeTypeSimpleDto>(TestContext.Current.CancellationToken);

        var request = new
        {
            scopeValue = "us-west",
            version = "1.0.0",
            content = "parameters:\n  setting1: value1\n",
            contentType = "application/x-yaml",
            isDraft = true
        };

        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{ScopeTypeDetails!.Id}/{configId}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParameterFileDto>(TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.ScopeValue.Should().Be("us-west");
        result.Status.Should().Be("Draft");
    }
}

public sealed class ValidationResultDto
{
    public required bool IsValid { get; init; }
    public ValidationErrorDto[]? Errors { get; init; }
}

public sealed class ParameterFileDto
{
    public required Guid Id { get; init; }
    public required Guid ScopeTypeId { get; init; }
    public required Guid ConfigurationId { get; init; }
    public string? ScopeValue { get; init; }
    public required string Version { get; init; }
    public required int MajorVersion { get; init; }
    public required string Checksum { get; init; }
    public required string Status { get; init; }
    public required bool IsPassthrough { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}

public sealed class ConfigurationSummaryDto
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool UseServerManagedParameters { get; init; }
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

public sealed class ScopeTypeSimpleDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
}

public sealed class ScopeValueSimpleDto
{
    public required Guid Id { get; init; }
    public required string Value { get; init; }
}

