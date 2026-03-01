// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Server.Endpoints;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

public class HealthEndpointsTests : IDisposable
{
    private readonly ServerWebApplicationFactory _factory = new();

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var health = await response.Content.ReadFromJsonAsync<HealthResponse>();
        health.Should().NotBeNull();
        health!.Status.Should().Be("Healthy");
        health.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Ready_ReturnsReady()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var readiness = await response.Content.ReadFromJsonAsync<ReadinessResponse>();
        readiness.Should().NotBeNull();
        readiness!.Status.Should().Be("Ready");
        readiness.Database.Should().Be("Connected");
        readiness.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    public void Dispose()
    {
        _factory.Dispose();
        GC.SuppressFinalize(this);
    }
}
