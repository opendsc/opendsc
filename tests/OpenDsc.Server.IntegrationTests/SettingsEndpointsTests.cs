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
        var result = await response.Content.ReadFromJsonAsync<RegistrationKeyResponse>();
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

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
