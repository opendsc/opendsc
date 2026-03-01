// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Server.Endpoints;
using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class RetentionEndpointsTests : IDisposable
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
    [Fact]
    public async Task CleanupConfigurationVersions_WithDryRun_ReturnsPreview()
    {
        using var client = CreateAuthenticatedClient();

        var request = new CleanupRequest
        {
            KeepVersions = 3,
            KeepDays = 30,
            DryRun = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/retention/configurations/cleanup", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VersionRetentionResult>();
        result.Should().NotBeNull();
        result!.IsDryRun.Should().BeTrue();
    }

    [Fact]
    public async Task CleanupConfigurationVersions_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var request = new CleanupRequest
        {
            KeepVersions = 3,
            KeepDays = 30,
            DryRun = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/retention/configurations/cleanup", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CleanupParameterVersions_WithDryRun_ReturnsPreview()
    {
        using var client = CreateAuthenticatedClient();

        var request = new CleanupRequest
        {
            KeepVersions = 2,
            KeepDays = 30,
            DryRun = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/retention/parameters/cleanup", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VersionRetentionResult>();
        result.Should().NotBeNull();
        result!.IsDryRun.Should().BeTrue();
    }

    [Fact]
    public async Task CleanupParameterVersions_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var request = new CleanupRequest
        {
            KeepVersions = 2,
            KeepDays = 30,
            DryRun = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/retention/parameters/cleanup", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
