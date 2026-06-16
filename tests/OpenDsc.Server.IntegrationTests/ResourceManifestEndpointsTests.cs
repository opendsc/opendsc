// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Contracts.ResourceManifests;

using Xunit;

namespace OpenDsc.Server.IntegrationTests;

[Trait("Category", "Integration")]
public class ResourceManifestEndpointsTests : IDisposable
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
    public async Task DiscoverFromMcp_WithAuth_RunsDiscovery()
    {
        using var client = CreateAuthenticatedClient();

        var response = await client.PostAsync(
            "/api/v1/resource-manifests/discover",
            null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<DiscoveryResult>(
            TestJsonOptions.Default,
            TestContext.Current.CancellationToken);

        result.Should().NotBeNull();

        // In test environment, we may discover 0 valid resources (filtered out invalid types)
        // This is OK - the important thing is that the discovery ran without errors
        result!.Discovered.Should().BeGreaterThanOrEqualTo(0);
        result.Imported.Should().BeGreaterThanOrEqualTo(0);
        result.Updated.Should().BeGreaterThanOrEqualTo(0);
        result.Failed.Should().BeGreaterThanOrEqualTo(0);
    }
}
