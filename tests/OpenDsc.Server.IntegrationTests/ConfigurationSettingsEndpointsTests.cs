// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Server.Endpoints;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class ConfigurationSettingsEndpointsTests : IDisposable
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

    // ---- GET /retention ----

    [Fact]
    public async Task GetConfigurationRetentionSettings_ReturnsNotOverridden_WhenNoOverrideSet()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/configurations/test-config/settings/retention", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<ConfigurationRetentionDto>(TestContext.Current.CancellationToken);
        dto.Should().NotBeNull();
        dto!.IsOverridden.Should().BeFalse();
        dto.KeepVersions.Should().BeNull();
        dto.KeepDays.Should().BeNull();
        dto.KeepReleaseVersions.Should().BeNull();
    }

    [Fact]
    public async Task GetConfigurationRetentionSettings_ReturnsNotFound_WhenConfigDoesNotExist()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/configurations/nonexistent-config/settings/retention", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetConfigurationRetentionSettings_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/configurations/test-config/settings/retention", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- PUT /retention ----

    [Fact]
    public async Task UpdateConfigurationRetentionSettings_PersistsOverrides()
    {
        using var client = CreateAuthenticatedClient();

        var request = new UpdateConfigurationRetentionRequest
        {
            KeepVersions = 5,
            KeepDays = 30,
            KeepReleaseVersions = false
        };

        var putResponse = await client.PutAsJsonAsync("/api/v1/configurations/test-config/settings/retention", request, TestContext.Current.CancellationToken);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync("/api/v1/configurations/test-config/settings/retention", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await getResponse.Content.ReadFromJsonAsync<ConfigurationRetentionDto>(TestContext.Current.CancellationToken);
        dto.Should().NotBeNull();
        dto!.IsOverridden.Should().BeTrue();
        dto.KeepVersions.Should().Be(5);
        dto.KeepDays.Should().Be(30);
        dto.KeepReleaseVersions.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateConfigurationRetentionSettings_PartialUpdate_LeavesOtherFieldsUnchanged()
    {
        using var client = CreateAuthenticatedClient();

        // Set initial overrides
        await client.PutAsJsonAsync("/api/v1/configurations/test-config/settings/retention",
            new UpdateConfigurationRetentionRequest { KeepVersions = 7, KeepDays = 60 }, TestContext.Current.CancellationToken);

        // Update only KeepVersions
        var putResponse = await client.PutAsJsonAsync("/api/v1/configurations/test-config/settings/retention",
            new UpdateConfigurationRetentionRequest { KeepVersions = 3 }, TestContext.Current.CancellationToken);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await putResponse.Content.ReadFromJsonAsync<ConfigurationRetentionDto>(TestContext.Current.CancellationToken);
        dto.Should().NotBeNull();
        dto!.KeepVersions.Should().Be(3);
        dto.KeepDays.Should().Be(60);
    }

    [Fact]
    public async Task UpdateConfigurationRetentionSettings_ReturnsNotFound_WhenConfigDoesNotExist()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.PutAsJsonAsync("/api/v1/configurations/nonexistent-config/settings/retention",
            new UpdateConfigurationRetentionRequest { KeepVersions = 5 }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateConfigurationRetentionSettings_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/v1/configurations/test-config/settings/retention",
            new UpdateConfigurationRetentionRequest { KeepVersions = 5 }, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---- DELETE /retention ----

    [Fact]
    public async Task DeleteConfigurationRetentionSettings_ClearsOverrides()
    {
        using var client = CreateAuthenticatedClient();

        // Set overrides first
        await client.PutAsJsonAsync("/api/v1/configurations/test-config/settings/retention",
            new UpdateConfigurationRetentionRequest { KeepVersions = 5, KeepDays = 30 }, TestContext.Current.CancellationToken);

        // Delete overrides
        var deleteResponse = await client.DeleteAsync("/api/v1/configurations/test-config/settings/retention", TestContext.Current.CancellationToken);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Confirm they're gone
        var getResponse = await client.GetAsync("/api/v1/configurations/test-config/settings/retention", TestContext.Current.CancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await getResponse.Content.ReadFromJsonAsync<ConfigurationRetentionDto>(TestContext.Current.CancellationToken);
        dto.Should().NotBeNull();
        dto!.IsOverridden.Should().BeFalse();
        dto.KeepVersions.Should().BeNull();
        dto.KeepDays.Should().BeNull();
    }

    [Fact]
    public async Task DeleteConfigurationRetentionSettings_WhenNoneSet_ReturnsNoContent()
    {
        using var client = CreateAuthenticatedClient();

        // Delete without having set overrides — should still succeed
        var response = await client.DeleteAsync("/api/v1/configurations/test-config/settings/retention", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteConfigurationRetentionSettings_ReturnsNotFound_WhenConfigDoesNotExist()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.DeleteAsync("/api/v1/configurations/nonexistent-config/settings/retention", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteConfigurationRetentionSettings_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/v1/configurations/test-config/settings/retention", TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
