// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Server.Endpoints;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class RetentionSettingsEndpointsTests : IDisposable
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
    public async Task GetRetentionSettings_ReturnsDefaults()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/v1/settings/retention");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<RetentionSettingsDto>();
        dto.Should().NotBeNull();
        dto!.Enabled.Should().BeFalse();
        dto.KeepVersions.Should().Be(10);
        dto.KeepDays.Should().Be(90);
        dto.KeepReleaseVersions.Should().BeTrue();
        dto.KeepArchivedVersions.Should().BeTrue();
        dto.ScheduleIntervalHours.Should().Be(24);
        dto.ReportKeepCount.Should().Be(1000);
        dto.ReportKeepDays.Should().Be(30);
        dto.StatusEventKeepCount.Should().Be(200);
        dto.StatusEventKeepDays.Should().Be(7);
    }

    [Fact]
    public async Task GetRetentionSettings_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/settings/retention");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateRetentionSettings_PersistsChanges()
    {
        using var client = CreateAuthenticatedClient();

        var request = new UpdateRetentionSettingsRequest
        {
            Enabled = true,
            KeepVersions = 5,
            KeepDays = 30,
            KeepReleaseVersions = false,
            KeepArchivedVersions = false,
            ScheduleIntervalHours = 12,
            ReportKeepCount = 500,
            ReportKeepDays = 60,
            StatusEventKeepCount = 100,
            StatusEventKeepDays = 3
        };

        var putResponse = await client.PutAsJsonAsync("/api/v1/settings/retention", request);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await client.GetAsync("/api/v1/settings/retention");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await getResponse.Content.ReadFromJsonAsync<RetentionSettingsDto>();
        dto.Should().NotBeNull();
        dto!.Enabled.Should().BeTrue();
        dto.KeepVersions.Should().Be(5);
        dto.KeepDays.Should().Be(30);
        dto.KeepReleaseVersions.Should().BeFalse();
        dto.KeepArchivedVersions.Should().BeFalse();
        dto.ScheduleIntervalHours.Should().Be(12);
        dto.ReportKeepCount.Should().Be(500);
        dto.ReportKeepDays.Should().Be(60);
        dto.StatusEventKeepCount.Should().Be(100);
        dto.StatusEventKeepDays.Should().Be(3);
    }

    [Fact]
    public async Task UpdateRetentionSettings_WithNullFields_LeavesValuesUnchanged()
    {
        using var client = CreateAuthenticatedClient();

        // First set a known state
        await client.PutAsJsonAsync("/api/v1/settings/retention", new UpdateRetentionSettingsRequest
        {
            KeepVersions = 7
        });

        // Update only one field
        var request = new UpdateRetentionSettingsRequest { KeepDays = 45 };
        var putResponse = await client.PutAsJsonAsync("/api/v1/settings/retention", request);
        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await putResponse.Content.ReadFromJsonAsync<RetentionSettingsDto>();
        dto.Should().NotBeNull();
        dto!.KeepVersions.Should().Be(7);
        dto.KeepDays.Should().Be(45);
    }

    [Fact]
    public async Task UpdateRetentionSettings_WithoutAuth_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var request = new UpdateRetentionSettingsRequest { Enabled = true };
        var response = await client.PutAsJsonAsync("/api/v1/settings/retention", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
