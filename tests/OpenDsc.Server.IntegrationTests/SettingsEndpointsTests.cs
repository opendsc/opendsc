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
public class SettingsEndpointsTests : IDisposable
{
    private readonly ServerWebApplicationFactory _factory = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private HttpClient CreateAuthenticatedClient()
    {
        return _factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task GetSettings_WithAdminAuth_ReturnsSettings()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<ServerSettingsResponse>(JsonOptions);
        settings.Should().NotBeNull();
    }

    [Fact]
    public async Task GetSettings_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RotateRegistrationKey_WithAdminAuth_GeneratesNewKey()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.PostAsync("/api/v1/settings/registration-keys", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegistrationKeyResponse>(JsonOptions);
        result.Should().NotBeNull();
        result!.Key.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RotateRegistrationKey_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/settings/registration-keys", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSettings_WithStalenessMultiplier_PersistsValue()
    {
        var client = CreateAuthenticatedClient();

        var updateRequest = new UpdateServerSettingsRequest { StalenessMultiplier = 3.5 };
        var updateResponse = await client.PutAsJsonAsync("/api/v1/settings", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ServerSettingsResponse>(JsonOptions);
        updated!.StalenessMultiplier.Should().Be(3.5);
    }

    // --- LCM Defaults ---

    [Fact]
    public async Task GetLcmDefaults_WithAdminAuth_ReturnsNullsInitially()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/settings/lcm-defaults");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var defaults = await response.Content.ReadFromJsonAsync<ServerLcmDefaultsResponse>(JsonOptions);
        defaults.Should().NotBeNull();
        defaults!.DefaultConfigurationMode.Should().BeNull();
        defaults.DefaultConfigurationModeInterval.Should().BeNull();
        defaults.DefaultReportCompliance.Should().BeNull();
    }

    [Fact]
    public async Task GetLcmDefaults_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/settings/lcm-defaults");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateLcmDefaults_WithAdminAuth_PersistsAllValues()
    {
        var client = CreateAuthenticatedClient();

        var request = new UpdateServerLcmDefaultsRequest
        {
            DefaultConfigurationMode = ConfigurationMode.Remediate,
            DefaultConfigurationModeInterval = TimeSpan.FromMinutes(30),
            DefaultReportCompliance = true
        };

        var updateResponse = await client.PutAsJsonAsync("/api/v1/settings/lcm-defaults", request);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ServerLcmDefaultsResponse>(JsonOptions);
        updated.Should().NotBeNull();
        updated!.DefaultConfigurationMode.Should().Be(ConfigurationMode.Remediate);
        updated.DefaultConfigurationModeInterval.Should().Be(TimeSpan.FromMinutes(30));
        updated.DefaultReportCompliance.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateLcmDefaults_WithNullValues_ClearsDefaults()
    {
        var client = CreateAuthenticatedClient();

        // Set values first
        await client.PutAsJsonAsync("/api/v1/settings/lcm-defaults", new UpdateServerLcmDefaultsRequest
        {
            DefaultConfigurationMode = ConfigurationMode.Monitor,
            DefaultConfigurationModeInterval = TimeSpan.FromMinutes(15),
            DefaultReportCompliance = false
        });

        // Clear them all
        var clearResponse = await client.PutAsJsonAsync("/api/v1/settings/lcm-defaults", new UpdateServerLcmDefaultsRequest
        {
            DefaultConfigurationMode = null,
            DefaultConfigurationModeInterval = null,
            DefaultReportCompliance = null
        });

        clearResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cleared = await clearResponse.Content.ReadFromJsonAsync<ServerLcmDefaultsResponse>(JsonOptions);
        cleared!.DefaultConfigurationMode.Should().BeNull();
        cleared.DefaultConfigurationModeInterval.Should().BeNull();
        cleared.DefaultReportCompliance.Should().BeNull();
    }

    [Fact]
    public async Task UpdateLcmDefaults_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/v1/settings/lcm-defaults", new UpdateServerLcmDefaultsRequest());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLcmDefaults_ReflectsPersistedValuesAfterUpdate()
    {
        var client = CreateAuthenticatedClient();

        var request = new UpdateServerLcmDefaultsRequest
        {
            DefaultConfigurationMode = ConfigurationMode.Monitor,
            DefaultConfigurationModeInterval = TimeSpan.FromMinutes(10),
            DefaultReportCompliance = true
        };

        await client.PutAsJsonAsync("/api/v1/settings/lcm-defaults", request);

        var getResponse = await client.GetAsync("/api/v1/settings/lcm-defaults");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ServerLcmDefaultsResponse>(JsonOptions);
        fetched!.DefaultConfigurationMode.Should().Be(ConfigurationMode.Monitor);
        fetched.DefaultConfigurationModeInterval.Should().Be(TimeSpan.FromMinutes(10));
        fetched.DefaultReportCompliance.Should().BeTrue();
    }

    // --- Public Settings ---

    [Fact]
    public async Task GetPublicSettings_WithoutAuth_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/settings/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetPublicSettings_ReturnsCertificateRotationInterval()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/settings/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<PublicSettingsResponse>(JsonOptions);
        settings.Should().NotBeNull();
        settings!.CertificateRotationInterval.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetPublicSettings_ReflectsUpdatedCertificateRotationInterval()
    {
        var adminClient = CreateAuthenticatedClient();
        var newInterval = TimeSpan.FromDays(30);

        var updateResponse = await adminClient.PutAsJsonAsync("/api/v1/settings",
            new UpdateServerSettingsRequest { CertificateRotationInterval = newInterval });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var publicClient = _factory.CreateClient();
        var response = await publicClient.GetAsync("/api/v1/settings/public");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<PublicSettingsResponse>(JsonOptions);
        settings!.CertificateRotationInterval.Should().Be(newInterval);
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
