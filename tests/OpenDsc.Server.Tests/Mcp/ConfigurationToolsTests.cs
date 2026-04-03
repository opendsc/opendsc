// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using OpenDsc.Lcm.Contracts;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Mcp;

using Xunit;

namespace OpenDsc.Server.Tests.Mcp;

[Trait("Category", "Unit")]
public class ConfigurationToolsTests : IDisposable
{
    private readonly ServerDbContext _db;
    private readonly ConfigurationTools _tools;

    public ConfigurationToolsTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ServerDbContext(options);
        _tools = new ConfigurationTools(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ListConfigurations_ReturnsEmpty_WhenNoneExist()
    {
        var result = await _tools.ListConfigurations(TestContext.Current.CancellationToken);

        result.Should().Contain("No configurations found");
    }

    [Fact]
    public async Task ListConfigurations_ReturnsConfigurations()
    {
        _db.Configurations.Add(new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "WebServer",
            Description = "Web server config",
            CreatedAt = DateTimeOffset.UtcNow
        });
        _db.Configurations.Add(new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "DatabaseServer",
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.ListConfigurations(TestContext.Current.CancellationToken);

        result.Should().Contain("Configurations (2)");
        result.Should().Contain("WebServer");
        result.Should().Contain("DatabaseServer");
    }

    [Fact]
    public async Task GetConfigurationDetails_ByName_ReturnsDetails()
    {
        var config = new Configuration
        {
            Id = Guid.NewGuid(),
            Name = "WebServer",
            Description = "Web server configuration",
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Configurations.Add(config);
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetConfigurationDetails("WebServer", TestContext.Current.CancellationToken);

        result.Should().Contain("WebServer");
        result.Should().Contain("Web server configuration");
    }

    [Fact]
    public async Task GetConfigurationDetails_ReturnsNotFound_WhenNotExists()
    {
        var result = await _tools.GetConfigurationDetails("NonExistent", TestContext.Current.CancellationToken);

        result.Should().Contain("not found");
    }

    [Fact]
    public async Task GetUnassignedNodes_ReturnsEmpty_WhenAllAssigned()
    {
        _db.Nodes.Add(CreateNode("web01.opendsc.dev", "WebServer"));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetUnassignedNodes(TestContext.Current.CancellationToken);

        result.Should().Contain("All nodes have a configuration assigned");
    }

    [Fact]
    public async Task GetUnassignedNodes_ReturnsUnassignedNodes()
    {
        _db.Nodes.Add(CreateNode("web01.opendsc.dev", "WebServer"));
        _db.Nodes.Add(CreateNode("orphan.opendsc.dev", null));
        await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _tools.GetUnassignedNodes(TestContext.Current.CancellationToken);

        result.Should().Contain("Unassigned Nodes (1)");
        result.Should().Contain("orphan.opendsc.dev");
        result.Should().NotContain("web01.opendsc.dev");
    }

    private static Node CreateNode(string fqdn, string? configName) => new()
    {
        Id = Guid.NewGuid(),
        Fqdn = fqdn,
        Status = NodeStatus.Unknown,
        ConfigurationName = configName,
        ConfigurationSource = ConfigurationSource.Pull,
        CertificateThumbprint = "test-thumbprint",
        CertificateSubject = "CN=test",
        CertificateNotAfter = DateTimeOffset.UtcNow.AddYears(1),
        CreatedAt = DateTimeOffset.UtcNow
    };
}
