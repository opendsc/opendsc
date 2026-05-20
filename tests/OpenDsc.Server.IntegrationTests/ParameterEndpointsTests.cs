// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Headers;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Parameters;
using OpenDsc.Contracts.Settings;
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
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParameterVersionDetails>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Version.Should().Be("1.0.0");
        result.Status.Should().Be(ParameterVersionStatus.Draft);
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
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
        };

        await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", createRequest, TestContext.Current.CancellationToken);

        // Act
        var response = await client.GetAsync($"/api/v1/parameters/{scopeTypeId}/{configId}/versions", TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var versions = await response.Content.ReadFromJsonAsync<List<ParameterVersionDetails>>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
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
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
        };

        await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", createRequest, TestContext.Current.CancellationToken);

        // Act
        var response = await client.PutAsync($"/api/v1/parameters/{scopeTypeId}/{configId}/versions/1.0.0/publish", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParameterVersionDetails>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.Version.Should().Be("1.0.0");
        result.Status.Should().Be(ParameterVersionStatus.Published);
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
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
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
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
        };

        await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", createRequest, TestContext.Current.CancellationToken);
        var publishResponse = await client.PutAsync($"/api/v1/parameters/{scopeTypeId}/{configId}/versions/1.0.0/publish", null, TestContext.Current.CancellationToken);
        if (!publishResponse.IsSuccessStatusCode)
        {
            var errorContent = await publishResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            throw new InvalidOperationException($"Publish failed with {publishResponse.StatusCode}: {errorContent}");
        }

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
        var registration = await registerResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
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

        // Create and publish a parameter file for the Default scope
        var defaultScopeId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var parameterRequest = new
        {
            version = "1.0.0",
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
        };
        var paramResponse = await client.PutAsJsonAsync($"/api/v1/parameters/{defaultScopeId}/{configId}", parameterRequest, TestContext.Current.CancellationToken);
        if (!paramResponse.IsSuccessStatusCode)
        {
            var errorContent = await paramResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            throw new InvalidOperationException($"Parameter creation failed: {paramResponse.StatusCode} - {errorContent}");
        }

        // Publish the parameter
        var publishResponse = await client.PutAsync($"/api/v1/parameters/{defaultScopeId}/{configId}/versions/1.0.0/publish", null, TestContext.Current.CancellationToken);
        if (!publishResponse.IsSuccessStatusCode)
        {
            var errorContent = await publishResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            throw new InvalidOperationException($"Parameter publish failed: {publishResponse.StatusCode} - {errorContent}");
        }

        // Act
        var response = await client.GetAsync($"/api/v1/nodes/{nodeId}/parameters/provenance?configurationId={configId}", TestContext.Current.CancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            throw new InvalidOperationException($"Provenance request failed: {response.StatusCode} - {errorContent}");
        }
        var result = await response.Content.ReadFromJsonAsync<ParameterProvenanceDetails>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.NodeId.Should().Be(nodeId);
        result.ConfigurationId.Should().Be(configId);
    }

    // TODO: ValidateParameterFile endpoint serialization issue needs investigation
    // [Fact]

    private async Task<(Guid ScopeTypeId, Guid ScopeValueId)> CreateRestrictedScopeTypeWithValueAsync(
        HttpClient client, string scopeTypeName, string scopeValue)
    {
        // Create restricted scope type
        var scopeTypeRequest = new { name = scopeTypeName, valueMode = "Restricted" };
        var scopeTypeResponse = await client.PostAsJsonAsync("/api/v1/scope-types", scopeTypeRequest);
        scopeTypeResponse.EnsureSuccessStatusCode();
        var ScopeTypeDetails = await scopeTypeResponse.Content.ReadFromJsonAsync<ScopeTypeDetails>(TestJsonOptions.Default);
        var scopeTypeId = ScopeTypeDetails!.Id;

        // Create scope value
        var scopeValueRequest = new { value = scopeValue };
        var scopeValueResponse = await client.PostAsJsonAsync($"/api/v1/scope-types/{scopeTypeId}/values", scopeValueRequest);
        scopeValueResponse.EnsureSuccessStatusCode();
        var ScopeValueDetails = await scopeValueResponse.Content.ReadFromJsonAsync<ScopeValueDetails>(TestJsonOptions.Default);
        var scopeValueId = ScopeValueDetails!.Id;

        return (scopeTypeId, scopeValueId);
    }

    private async Task<Guid> GetNodeScopeTypeIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/scope-types", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var scopeTypes = await response.Content.ReadFromJsonAsync<List<ScopeTypeDetails>>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
        var nodeScope = scopeTypes?.FirstOrDefault(st => st.Name == "Node");
        if (nodeScope is null)
        {
            throw new InvalidOperationException("Node scope type not found in database");
        }
        return nodeScope.Id;
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
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{scopeTypeId}/{configId}", request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParameterVersionDetails>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.ScopeValue.Should().Be("Development");
        result.Status.Should().Be(ParameterVersionStatus.Draft);
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
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
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
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
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

        var nodeScopeTypeId = await GetNodeScopeTypeIdAsync(client);

        var request = new
        {
            // scopeValue intentionally omitted
            version = "1.0.0",
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
        };

        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{nodeScopeTypeId}/{configId}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrUpdateParameter_WithNodeScopeType_AndUnregisteredNode_ReturnsBadRequest()
    {
        using var client = CreateAuthenticatedClient();
        var configId = await CreateTestConfigurationAsync(client, $"test-config-{Guid.NewGuid()}");

        var nodeScopeTypeId = await GetNodeScopeTypeIdAsync(client);

        var request = new
        {
            scopeValue = "not-registered.example.com",
            version = "1.0.0",
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
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
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
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
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
        };

        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{defaultScopeTypeId}/{configId}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParameterVersionDetails>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.ScopeValue.Should().BeNullOrEmpty();
        result.Status.Should().Be(ParameterVersionStatus.Draft);
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
        var ScopeTypeDetails = await scopeTypeResponse.Content.ReadFromJsonAsync<ScopeTypeDetails>(TestJsonOptions.Default, TestContext.Current.CancellationToken);

        var request = new
        {
            // scopeValue intentionally omitted
            version = "1.0.0",
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
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
        var ScopeTypeDetails = await scopeTypeResponse.Content.ReadFromJsonAsync<ScopeTypeDetails>(TestJsonOptions.Default, TestContext.Current.CancellationToken);

        var request = new
        {
            scopeValue = "us-west",
            version = "1.0.0",
            content = "parameters:\n  param1: value1\n  param2: value2\n  setting1: test\n  appName: TestApp\n  port: 8080",
            contentType = "application/x-yaml"
        };

        var response = await client.PutAsJsonAsync($"/api/v1/parameters/{ScopeTypeDetails!.Id}/{configId}", request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ParameterVersionDetails>(TestJsonOptions.Default, TestContext.Current.CancellationToken);
        result.Should().NotBeNull();
        result!.ScopeValue.Should().Be("us-west");
        result.Status.Should().Be(ParameterVersionStatus.Draft);
    }
}

