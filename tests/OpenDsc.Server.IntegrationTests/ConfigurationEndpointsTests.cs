// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Headers;

using FluentAssertions;

using OpenDsc.Server.Contracts;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class ConfigurationEndpointsTests : IDisposable
{
    private readonly ServerWebApplicationFactory _factory = new();

    public void Dispose()
    {
        _factory?.Dispose();
        GC.SuppressFinalize(this);
    }

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-admin-key");
        return client;
    }

    [Fact]
    public async Task GetConfigurations_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/configurations");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetConfigurations_WithAuth_ReturnsEmptyList()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/configurations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var configs = await response.Content.ReadFromJsonAsync<List<ConfigurationSummary>>();
        configs.Should().NotBeNull();
        configs.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateConfiguration_WithValidData_ReturnsCreated()
    {
        using var client = CreateAuthenticatedClient();

        var request = new CreateConfigurationRequest
        {
            Name = "test-config",
            Content = "$schema: https://schema.management.azure.com/dsc/2023/10/config.json\nresources: []"
        };

        var response = await client.PostAsJsonAsync("/api/v1/configurations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/configurations/test-config");
    }

    [Fact]
    public async Task CreateConfiguration_Duplicate_ReturnsConflict()
    {
        using var client = CreateAuthenticatedClient();

        var request = new CreateConfigurationRequest
        {
            Name = "duplicate-config",
            Content = "$schema: https://schema.management.azure.com/dsc/2023/10/config.json\nresources: []"
        };

        await client.PostAsJsonAsync("/api/v1/configurations", request);
        var response = await client.PostAsJsonAsync("/api/v1/configurations", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetConfiguration_Existing_ReturnsConfiguration()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateConfigurationRequest
        {
            Name = "get-config-test",
            Content = "$schema: https://schema.management.azure.com/dsc/2023/10/config.json\nresources: []"
        };
        await client.PostAsJsonAsync("/api/v1/configurations", createRequest);

        var response = await client.GetAsync("/api/v1/configurations/get-config-test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var config = await response.Content.ReadFromJsonAsync<ConfigurationDetails>();
        config.Should().NotBeNull();
        config!.Name.Should().Be("get-config-test");
        config.Content.Should().Contain("resources: []");
    }

    [Fact]
    public async Task GetConfiguration_NotFound_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/configurations/non-existent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateConfiguration_Existing_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateConfigurationRequest
        {
            Name = "update-config-test",
            Content = "$schema: https://schema.management.azure.com/dsc/2023/10/config.json\nresources: []"
        };
        await client.PostAsJsonAsync("/api/v1/configurations", createRequest);

        var updateRequest = new UpdateConfigurationRequest
        {
            Content = "$schema: https://schema.management.azure.com/dsc/2023/10/config.json\nresources:\n  - type: Test"
        };
        var response = await client.PutAsJsonAsync("/api/v1/configurations/update-config-test", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync("/api/v1/configurations/update-config-test");
        var config = await getResponse.Content.ReadFromJsonAsync<ConfigurationDetails>();
        config!.Content.Should().Contain("type: Test");
    }

    [Fact]
    public async Task UpdateConfiguration_NotFound_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var updateRequest = new UpdateConfigurationRequest
        {
            Content = "$schema: https://schema.management.azure.com/dsc/2023/10/config.json\nresources: []"
        };
        var response = await client.PutAsJsonAsync("/api/v1/configurations/non-existent", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteConfiguration_Existing_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();

        var createRequest = new CreateConfigurationRequest
        {
            Name = "delete-config-test",
            Content = "$schema: https://schema.management.azure.com/dsc/2023/10/config.json\nresources: []"
        };
        await client.PostAsJsonAsync("/api/v1/configurations", createRequest);

        var response = await client.DeleteAsync("/api/v1/configurations/delete-config-test");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await client.GetAsync("/api/v1/configurations/delete-config-test");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteConfiguration_NotFound_ReturnsNotFound()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/v1/configurations/non-existent");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConfigurations_AfterCreating_ReturnsAll()
    {
        using var client = CreateAuthenticatedClient();

        await client.PostAsJsonAsync("/api/v1/configurations", new CreateConfigurationRequest
        {
            Name = "config1",
            Content = "content1"
        });
        await client.PostAsJsonAsync("/api/v1/configurations", new CreateConfigurationRequest
        {
            Name = "config2",
            Content = "content2"
        });

        var response = await client.GetAsync("/api/v1/configurations");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var configs = await response.Content.ReadFromJsonAsync<List<ConfigurationSummary>>();
        configs.Should().NotBeNull();
        configs!.Count.Should().BeGreaterOrEqualTo(2);
        configs.Should().Contain(c => c.Name == "config1");
        configs.Should().Contain(c => c.Name == "config2");
    }
}
