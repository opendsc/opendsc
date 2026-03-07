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

    [Fact]
    public async Task CleanupConfigurationVersions_WithKeepReleaseAndArchivedFlags_ReturnsOk()
    {
        using var client = CreateAuthenticatedClient();

        var request = new CleanupRequest
        {
            KeepVersions = 5,
            KeepDays = 60,
            KeepReleaseVersions = false,
            KeepArchivedVersions = false,
            DryRun = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/retention/configurations/cleanup", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VersionRetentionResult>();
        result.Should().NotBeNull();
        result!.IsDryRun.Should().BeTrue();
    }

    [Fact]
    public async Task CleanupCompositeConfigurationVersions_WithDryRun_ReturnsPreview()
    {
        using var client = CreateAuthenticatedClient();

        var request = new CleanupRequest
        {
            KeepVersions = 3,
            KeepDays = 30,
            DryRun = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/retention/composite-configurations/cleanup", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VersionRetentionResult>();
        result.Should().NotBeNull();
        result!.IsDryRun.Should().BeTrue();
    }

    [Fact]
    public async Task CleanupCompositeConfigurationVersions_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var request = new CleanupRequest { KeepVersions = 3, KeepDays = 30, DryRun = true };

        var response = await client.PostAsJsonAsync("/api/v1/retention/composite-configurations/cleanup", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetRunHistory_ReturnsEmptyList_WhenNoRunsExist()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/retention/runs");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var runs = await response.Content.ReadFromJsonAsync<List<RetentionRunDto>>();
        runs.Should().NotBeNull();
        runs!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRunHistory_AfterCleanup_ContainsRunRecord()
    {
        using var client = CreateAuthenticatedClient();

        var request = new CleanupRequest { KeepVersions = 10, KeepDays = 90, DryRun = false };
        await client.PostAsJsonAsync("/api/v1/retention/configurations/cleanup", request);

        var response = await client.GetAsync("/api/v1/retention/runs?limit=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var runs = await response.Content.ReadFromJsonAsync<List<RetentionRunDto>>();
        runs.Should().NotBeNull();
        runs!.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetRunHistory_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/retention/runs");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CleanupReports_WithDryRun_ReturnsPreview()
    {
        using var client = CreateAuthenticatedClient();

        var request = new RecordCleanupRequest
        {
            KeepCount = 100,
            KeepDays = 30,
            DryRun = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/retention/reports/cleanup", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VersionRetentionResult>();
        result.Should().NotBeNull();
        result!.IsDryRun.Should().BeTrue();
    }

    [Fact]
    public async Task CleanupReports_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var request = new RecordCleanupRequest { KeepCount = 100, KeepDays = 30, DryRun = true };

        var response = await client.PostAsJsonAsync("/api/v1/retention/reports/cleanup", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CleanupNodeStatusEvents_WithDryRun_ReturnsPreview()
    {
        using var client = CreateAuthenticatedClient();

        var request = new RecordCleanupRequest
        {
            KeepCount = 50,
            KeepDays = 7,
            DryRun = true
        };

        var response = await client.PostAsJsonAsync("/api/v1/retention/status-events/cleanup", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<VersionRetentionResult>();
        result.Should().NotBeNull();
        result!.IsDryRun.Should().BeTrue();
    }

    [Fact]
    public async Task CleanupNodeStatusEvents_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var request = new RecordCleanupRequest { KeepCount = 50, KeepDays = 7, DryRun = true };

        var response = await client.PostAsJsonAsync("/api/v1/retention/status-events/cleanup", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

