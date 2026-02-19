// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net;

using AwesomeAssertions;

using OpenDsc.Server.Contracts;
using OpenDsc.Server.FunctionalTests.DatabaseProviders;

using Xunit;

namespace OpenDsc.Server.FunctionalTests;

[Trait("Category", "Functional")]
public abstract class NodeRegistrationTests
{
    protected readonly DatabaseProviderFixture Fixture;
    protected readonly HttpClient Client;

    protected NodeRegistrationTests(DatabaseProviderFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateClient();
    }

    [Fact]
    public async Task RegisterNode_WithValidRegistrationKey_ReturnsNodeIdOnly()
    {
        var request = new RegisterNodeRequest
        {
            Fqdn = $"test-node-{Guid.NewGuid()}.example.com",
            RegistrationKey = "test-registration-key"
        };

        var response = await Client.PostAsJsonAsync("/api/v1/nodes/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<RegisterNodeResponse>();
        result.Should().NotBeNull();
        result!.NodeId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RegisterNode_WithInvalidRegistrationKey_ReturnsBadRequest()
    {
        var request = new RegisterNodeRequest
        {
            Fqdn = $"test-node-{Guid.NewGuid()}.example.com",
            RegistrationKey = "invalid-key"
        };

        var response = await Client.PostAsJsonAsync("/api/v1/nodes/register", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RegisterNode_ReRegistrationOfExistingNode_ReturnsSameNodeId()
    {
        var fqdn = $"test-node-{Guid.NewGuid()}.example.com";
        var request = new RegisterNodeRequest
        {
            Fqdn = fqdn,
            RegistrationKey = "test-registration-key"
        };

        var firstResponse = await Client.PostAsJsonAsync("/api/v1/nodes/register", request);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        var secondResponse = await Client.PostAsJsonAsync("/api/v1/nodes/register", request);
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<RegisterNodeResponse>();

        secondResult.Should().NotBeNull();
        secondResult!.NodeId.Should().Be(firstResult!.NodeId);
    }

    [Fact]
    public async Task GetNodes_WithAdminAuthentication_ReturnsNodeList()
    {
        var registerRequest = new RegisterNodeRequest
        {
            Fqdn = $"list-test-{Guid.NewGuid()}.example.com",
            RegistrationKey = "test-registration-key"
        };
        await Client.PostAsJsonAsync("/api/v1/nodes/register", registerRequest);

        using var adminClient = await AuthenticationHelper.CreateAuthenticatedClientAsync(Fixture);

        var response = await adminClient.GetAsync("/api/v1/nodes/");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var nodes = await response.Content.ReadFromJsonAsync<List<NodeSummary>>();
        nodes.Should().NotBeNull();
        nodes!.Should().NotBeEmpty();
        nodes.Should().Contain(n => n.Fqdn == registerRequest.Fqdn);
    }
}

[Collection("SQLite")]
public class SqliteNodeRegistrationTests(SqliteProviderFixture fixture) : NodeRegistrationTests(fixture)
{
}

[Collection("SQL Server")]
public class SqlServerNodeRegistrationTests(SqlServerProviderFixture fixture) : NodeRegistrationTests(fixture)
{
}

[Collection("PostgreSQL")]
public class PostgreSqlNodeRegistrationTests(PostgreSqlProviderFixture fixture) : NodeRegistrationTests(fixture)
{
}
