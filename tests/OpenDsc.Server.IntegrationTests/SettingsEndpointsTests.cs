// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;
using System.Net.Http.Headers;

using FluentAssertions;

using OpenDsc.Server.Contracts;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

public class SettingsEndpointsTests : IDisposable
{
    private readonly ServerWebApplicationFactory _factory = new();

    private HttpClient CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-admin-key");
        return client;
    }

    [Fact]
    public async Task GetSettings_WithAdminAuth_ReturnsSettings()
    {
        var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/settings");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var settings = await response.Content.ReadFromJsonAsync<ServerSettingsResponse>();
        settings.Should().NotBeNull();
        settings!.RegistrationKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSettings_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateSettings_WithAdminAuth_UpdatesSettings()
    {
        var client = CreateAuthenticatedClient();

        var updateRequest = new UpdateServerSettingsRequest
        {
            KeyRotationInterval = TimeSpan.FromDays(30)
        };

        var response = await client.PutAsJsonAsync("/api/v1/settings", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ServerSettingsResponse>();
        result!.KeyRotationInterval.Should().Be(TimeSpan.FromDays(30));
    }

    [Fact]
    public async Task UpdateSettings_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var updateRequest = new UpdateServerSettingsRequest
        {
            KeyRotationInterval = TimeSpan.FromDays(30)
        };

        var response = await client.PutAsJsonAsync("/api/v1/settings", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RotateRegistrationKey_WithAdminAuth_GeneratesNewKey()
    {
        var client = CreateAuthenticatedClient();

        var getBeforeResponse = await client.GetAsync("/api/v1/settings");
        var settingsBefore = await getBeforeResponse.Content.ReadFromJsonAsync<ServerSettingsResponse>();
        var oldKey = settingsBefore!.RegistrationKey;

        var response = await client.PostAsync("/api/v1/settings/registration-key/rotate", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RotateRegistrationKeyResponse>();
        result!.RegistrationKey.Should().NotBe(oldKey);
        result.RegistrationKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RotateRegistrationKey_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/settings/registration-key/rotate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
