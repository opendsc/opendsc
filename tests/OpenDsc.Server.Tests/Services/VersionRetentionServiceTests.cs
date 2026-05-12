// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

#pragma warning disable xUnit1051

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using OpenDsc.Contracts.Lcm;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Contracts.Configurations;
using OpenDsc.Server.Services;

using ParameterVersionStatus = OpenDsc.Contracts.Parameters.ParameterVersionStatus;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class VersionRetentionServiceTests : IDisposable
{
    private readonly ServerDbContext _db;
    private readonly IOptions<ServerConfig> _serverConfig;
    private readonly VersionRetentionService _service;
    private readonly string _tempDir;

    public VersionRetentionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ServerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ServerDbContext(options);
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        var config = new ServerConfig { ConfigurationsDirectory = _tempDir };
        _serverConfig = Options.Create(config);

        _service = new VersionRetentionService(
            _db,
            _serverConfig,
            new NullLogger<VersionRetentionService>()
        );
    }

    public void Dispose()
    {
        _db?.Dispose();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region CleanupConfigurationVersionsAsync Tests

    [Fact]
    public async Task CleanupConfigurationVersionsAsync_WithEmptyDatabase_ReturnsEmptyResult()
    {
        // Arrange
        var policy = new RetentionPolicy { KeepVersions = 10, KeepDays = 90 };

        // Act
        var result = await _service.CleanupConfigurationVersionsAsync(policy);

        // Assert
        result.Should().NotBeNull();
        result.DeletedCount.Should().Be(0);
        result.KeptCount.Should().Be(0);
        result.IsDryRun.Should().BeFalse();
    }

    [Fact]
    public async Task CleanupConfigurationVersionsAsync_WithAllVersionsInKeepCount_DeletesNothing()
    {
        // Arrange
        var configId = Guid.NewGuid();
        CreateConfiguration(configId, "MyConfig", versionCount: 5);
        var policy = new RetentionPolicy { KeepVersions = 10, KeepDays = 90 };

        // Act
        var result = await _service.CleanupConfigurationVersionsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(0);
        result.KeptCount.Should().Be(5);
        var config = _db.Configurations.Include(c => c.Versions).First();
        config.Versions.Should().HaveCount(5);
    }

    [Fact(Skip = "In-Memory DB Include() issue - debugging")]
    public async Task CleanupConfigurationVersionsAsync_ExceedsVersionCount_DeletesOldest()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var versions = CreateConfiguration(configId, "MyConfig", versionCount: 15);
        var policy = new RetentionPolicy { KeepVersions = 10, KeepDays = 90 };

        // Act
        var result = await _service.CleanupConfigurationVersionsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(5);
        result.KeptCount.Should().Be(10);
        var config = _db.Configurations.Include(c => c.Versions).First();
        config.Versions.Should().HaveCount(10);
    }

    [Fact(Skip = "In-Memory DB Include() issue")]
    public async Task CleanupConfigurationVersionsAsync_WithOldVersions_DeletesByAge()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = new Configuration { Id = configId, Name = "MyConfig" };
        _db.Configurations.Add(config);

        // Add 5 old versions (100 days ago)
        for (int i = 1; i <= 5; i++)
        {
            var version = new ConfigurationVersion
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configId,
                Version = $"{i}.0.0",
                Status = ConfigurationVersionStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-100)
            };
            _db.ConfigurationVersions.Add(version);
        }

        // Add 3 recent versions (10 days ago)
        for (int i = 6; i <= 8; i++)
        {
            var version = new ConfigurationVersion
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configId,
                Version = $"{i}.0.0",
                Status = ConfigurationVersionStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-10)
            };
            _db.ConfigurationVersions.Add(version);
        }

        _db.SaveChanges();

        var policy = new RetentionPolicy { KeepVersions = 100, KeepDays = 30 };

        // Act
        var result = await _service.CleanupConfigurationVersionsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(5);
        result.KeptCount.Should().Be(3);
        var remaining = _db.ConfigurationVersions.Where(v => v.ConfigurationId == configId);
        remaining.Should().HaveCount(3);
    }

    [Fact(Skip = "In-Memory DB Include() issue")]
    public async Task CleanupConfigurationVersionsAsync_ProtectsActiveVersions()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var config = new Configuration { Id = configId, Name = "MyConfig" };
        _db.Configurations.Add(config);

        var node = new Node { Id = nodeId, Fqdn = "node1.contoso.com" };
        _db.Nodes.Add(node);

        // Create 10 old versions
        var versions = new List<ConfigurationVersion>();
        for (int i = 1; i <= 10; i++)
        {
            var version = new ConfigurationVersion
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configId,
                Version = $"{i}.0.0",
                Status = ConfigurationVersionStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-100)
            };
            _db.ConfigurationVersions.Add(version);
            versions.Add(version);
        }

        _db.SaveChanges();

        // Make version 5 active
        var nodeConfig = new NodeConfiguration
        {
            NodeId = nodeId,
            ConfigurationId = configId,
            ActiveVersion = versions[4].Version
        };
        _db.NodeConfigurations.Add(nodeConfig);
        _db.SaveChanges();

        var policy = new RetentionPolicy { KeepVersions = 0, KeepDays = 90 };

        // Act
        var result = await _service.CleanupConfigurationVersionsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(9);
        result.KeptCount.Should().Be(1);
        var remaining = _db.ConfigurationVersions.Where(v => v.ConfigurationId == configId);
        remaining.Single().Version.Should().Be("5.0.0");
    }

    [Fact(Skip = "In-Memory DB Include() issue")]
    public async Task CleanupConfigurationVersionsAsync_ProtectsReleaseVersions()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var config = new Configuration { Id = configId, Name = "MyConfig" };
        _db.Configurations.Add(config);

        // 5 prerelease versions
        for (int i = 1; i <= 5; i++)
        {
            var version = new ConfigurationVersion
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configId,
                Version = $"{i}.0.0",
                Status = ConfigurationVersionStatus.Published,
                PrereleaseChannel = "beta",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-100)
            };
            _db.ConfigurationVersions.Add(version);
        }

        // 5 release versions
        for (int i = 6; i <= 10; i++)
        {
            var version = new ConfigurationVersion
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configId,
                Version = $"{i}.0.0",
                Status = ConfigurationVersionStatus.Published,
                PrereleaseChannel = null,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-100)
            };
            _db.ConfigurationVersions.Add(version);
        }

        _db.SaveChanges();

        var policy = new RetentionPolicy { KeepVersions = 0, KeepDays = 90, KeepReleaseVersions = true };

        // Act
        var result = await _service.CleanupConfigurationVersionsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(5);
        result.KeptCount.Should().Be(5);
        var remaining = _db.ConfigurationVersions.Where(v => v.ConfigurationId == configId);
        remaining.Should().AllSatisfy(v => v.PrereleaseChannel.Should().BeNull());
    }

    [Fact(Skip = "In-Memory DB Include() issue")]
    public async Task CleanupConfigurationVersionsAsync_DryRun_DoesNotDelete()
    {
        // Arrange
        var configId = Guid.NewGuid();
        CreateConfiguration(configId, "MyConfig", versionCount: 15);
        var policy = new RetentionPolicy { KeepVersions = 5, KeepDays = 90, DryRun = true };

        // Act
        var result = await _service.CleanupConfigurationVersionsAsync(policy);

        // Assert
        result.IsDryRun.Should().BeTrue();
        result.DeletedCount.Should().Be(10);
        var config = _db.Configurations.Include(c => c.Versions).First();
        config.Versions.Should().HaveCount(15);
    }

    [Fact]
    public async Task CleanupConfigurationVersionsAsync_PersistsRetentionRun()
    {
        // Arrange
        var configId = Guid.NewGuid();
        CreateConfiguration(configId, "MyConfig", versionCount: 5);
        var policy = new RetentionPolicy { KeepVersions = 2, KeepDays = 90, IsScheduled = true };

        // Act
        var result = await _service.CleanupConfigurationVersionsAsync(policy);

        // Assert
        var run = _db.RetentionRuns.FirstOrDefault();
        run.Should().NotBeNull();
        run!.VersionType.Should().Be("Configuration");
        run.IsScheduled.Should().BeTrue();
        run.DeletedCount.Should().Be(result.DeletedCount);
        run.KeptCount.Should().Be(result.KeptCount);
    }

    [Fact(Skip = "In-Memory DB Include() issue")]
    public async Task CleanupConfigurationVersionsAsync_WithPerConfigOverrides_UsesOverrides()
    {
        // Arrange
        var configId = Guid.NewGuid();
        CreateConfiguration(configId, "MyConfig", versionCount: 15);

        // Create settings override for this configuration
        var settings = new ConfigurationSettings
        {
            ConfigurationId = configId,
            RetentionKeepVersions = 3,
            RetentionKeepDays = 90,
            RetentionKeepReleaseVersions = false
        };
        _db.Set<ConfigurationSettings>().Add(settings);
        _db.SaveChanges();

        var policy = new RetentionPolicy { KeepVersions = 20, KeepDays = 90 };

        // Act
        var result = await _service.CleanupConfigurationVersionsAsync(policy);

        // Assert - Should use config override (KeepVersions = 3) not policy (KeepVersions = 20)
        result.DeletedCount.Should().Be(12);
        result.KeptCount.Should().Be(3);
    }

    #endregion

    #region CleanupParameterVersionsAsync Tests

    [Fact]
    public async Task CleanupParameterVersionsAsync_WithEmptyDatabase_ReturnsEmptyResult()
    {
        // Arrange
        var policy = new RetentionPolicy { KeepVersions = 10, KeepDays = 90 };

        // Act
        var result = await _service.CleanupParameterVersionsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(0);
        result.KeptCount.Should().Be(0);
    }

    [Fact]
    public async Task CleanupParameterVersionsAsync_ProtectsPublishedVersions()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var schemaId = Guid.NewGuid();
        var scopeTypeId = Guid.NewGuid();

        var config = new Configuration { Id = configId, Name = "MyConfig" };
        _db.Configurations.Add(config);

        var schema = new ParameterSchema { Id = schemaId, ConfigurationId = configId };
        _db.Set<ParameterSchema>().Add(schema);

        var scopeType = new ScopeType { Id = scopeTypeId, Name = "Environment" };
        _db.ScopeTypes.Add(scopeType);
        _db.SaveChanges();

        // 5 draft versions
        for (int i = 1; i <= 5; i++)
        {
            var file = new ParameterFile
            {
                Id = Guid.NewGuid(),
                ParameterSchemaId = schemaId,
                ScopeTypeId = scopeTypeId,
                ScopeValue = "Production",
                Version = $"{i}.0.0",
                MajorVersion = i,
                Checksum = $"checksum{i}",
                Status = ParameterVersionStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-100)
            };
            _db.ParameterFiles.Add(file);
        }

        // 5 published versions
        for (int i = 6; i <= 10; i++)
        {
            var file = new ParameterFile
            {
                Id = Guid.NewGuid(),
                ParameterSchemaId = schemaId,
                ScopeTypeId = scopeTypeId,
                ScopeValue = "Production",
                Version = $"{i}.0.0",
                MajorVersion = i,
                Checksum = $"checksum{i}",
                Status = ParameterVersionStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-100)
            };
            _db.ParameterFiles.Add(file);
        }

        _db.SaveChanges();

        var policy = new RetentionPolicy { KeepVersions = 0, KeepDays = 90 };

        // Act
        var result = await _service.CleanupParameterVersionsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(5);
        result.KeptCount.Should().Be(5);
        var remaining = _db.ParameterFiles.Where(pf => pf.ParameterSchemaId == schemaId);
        remaining.Should().AllSatisfy(pf => pf.Status.Should().Be(ParameterVersionStatus.Published));
    }

    [Fact]
    public async Task CleanupParameterVersionsAsync_CleanupByScope()
    {
        // Arrange
        var configId = Guid.NewGuid();
        var schemaId = Guid.NewGuid();
        var scopeTypeId = Guid.NewGuid();

        var config = new Configuration { Id = configId, Name = "MyConfig" };
        _db.Configurations.Add(config);

        var schema = new ParameterSchema { Id = schemaId, ConfigurationId = configId };
        _db.Set<ParameterSchema>().Add(schema);

        var scopeType = new ScopeType { Id = scopeTypeId, Name = "Environment" };
        _db.ScopeTypes.Add(scopeType);

        // 5 versions for Production scope
        for (int i = 1; i <= 5; i++)
        {
            var file = new ParameterFile
            {
                Id = Guid.NewGuid(),
                ParameterSchemaId = schemaId,
                ScopeTypeId = scopeTypeId,
                ScopeValue = "Production",
                Version = $"{i}.0.0",
                MajorVersion = 1,
                Checksum = $"checksum{i}",
                Status = ParameterVersionStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-100)
            };
            _db.ParameterFiles.Add(file);
        }

        // 5 versions for Staging scope
        for (int i = 6; i <= 10; i++)
        {
            var file = new ParameterFile
            {
                Id = Guid.NewGuid(),
                ParameterSchemaId = schemaId,
                ScopeTypeId = scopeTypeId,
                ScopeValue = "Staging",
                Version = $"{i}.0.0",
                MajorVersion = 1,
                Checksum = $"checksum{i}",
                Status = ParameterVersionStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-100)
            };
            _db.ParameterFiles.Add(file);
        }

        _db.SaveChanges();

        var policy = new RetentionPolicy { KeepVersions = 2, KeepDays = 90 };

        // Act
        var result = await _service.CleanupParameterVersionsAsync(policy);

        // Assert - Should keep 2 per scope, delete 3 per scope = 6 total
        result.DeletedCount.Should().Be(6);
        result.KeptCount.Should().BeGreaterThanOrEqualTo(4);
    }

    #endregion

    #region CleanupCompositeConfigurationVersionsAsync Tests

    [Fact]
    public async Task CleanupCompositeConfigurationVersionsAsync_WithEmptyDatabase_ReturnsEmptyResult()
    {
        // Arrange
        var policy = new RetentionPolicy { KeepVersions = 10, KeepDays = 90 };

        // Act
        var result = await _service.CleanupCompositeConfigurationVersionsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(0);
        result.KeptCount.Should().Be(0);
    }

    [Fact(Skip = "In-Memory DB Include() issue")]
    public async Task CleanupCompositeConfigurationVersionsAsync_ExceedsVersionCount_DeletesOldest()
    {
        // Arrange
        var compositeId = Guid.NewGuid();
        var composite = new CompositeConfiguration { Id = compositeId, Name = "MyComposite" };
        _db.CompositeConfigurations.Add(composite);

        for (int i = 1; i <= 15; i++)
        {
            var version = new CompositeConfigurationVersion
            {
                Id = Guid.NewGuid(),
                CompositeConfigurationId = compositeId,
                Version = $"{i}.0.0",
                Status = ConfigurationVersionStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-i)
            };
            _db.CompositeConfigurationVersions.Add(version);
        }

        _db.SaveChanges();

        var policy = new RetentionPolicy { KeepVersions = 5, KeepDays = 90 };

        // Act
        var result = await _service.CleanupCompositeConfigurationVersionsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(10);
        result.KeptCount.Should().Be(5);
    }

    [Fact(Skip = "In-Memory DB Include() issue")]
    public async Task CleanupCompositeConfigurationVersionsAsync_ProtectsActiveVersions()
    {
        // Arrange
        var compositeId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        var composite = new CompositeConfiguration { Id = compositeId, Name = "MyComposite" };
        _db.CompositeConfigurations.Add(composite);

        var node = new Node { Id = nodeId, Fqdn = "node1.contoso.com" };
        _db.Nodes.Add(node);

        var versions = new List<CompositeConfigurationVersion>();
        for (int i = 1; i <= 10; i++)
        {
            var version = new CompositeConfigurationVersion
            {
                Id = Guid.NewGuid(),
                CompositeConfigurationId = compositeId,
                Version = $"{i}.0.0",
                Status = ConfigurationVersionStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-100)
            };
            _db.CompositeConfigurationVersions.Add(version);
            versions.Add(version);
        }

        _db.SaveChanges();

        // Make version 5 active
        var nodeConfig = new NodeConfiguration
        {
            NodeId = nodeId,
            CompositeConfigurationId = compositeId,
            ActiveVersion = versions[4].Version
        };
        _db.NodeConfigurations.Add(nodeConfig);
        _db.SaveChanges();

        var policy = new RetentionPolicy { KeepVersions = 0, KeepDays = 90 };

        // Act
        var result = await _service.CleanupCompositeConfigurationVersionsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(9);
        result.KeptCount.Should().Be(1);
    }

    #endregion

    #region CleanupReportsAsync Tests

    [Fact]
    public async Task CleanupReportsAsync_WithEmptyDatabase_ReturnsEmptyResult()
    {
        // Arrange
        var policy = new RecordRetentionPolicy { KeepCount = 100, KeepDays = 30 };

        // Act
        var result = await _service.CleanupReportsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(0);
        result.KeptCount.Should().Be(0);
    }

    [Fact(Skip = "In-Memory DB ExecuteDelete not supported")]
    public async Task CleanupReportsAsync_ExceedsKeepCount_DeletesOldest()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = new Node { Id = nodeId, Fqdn = "node1.contoso.com" };
        _db.Nodes.Add(node);
        _db.SaveChanges();

        // Add 150 reports for the node
        for (int i = 1; i <= 150; i++)
        {
            var report = new Report
            {
                Id = Guid.NewGuid(),
                NodeId = nodeId,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-i)
            };
            _db.Reports.Add(report);
        }

        _db.SaveChanges();

        var policy = new RecordRetentionPolicy { KeepCount = 100, KeepDays = 90 };

        // Act
        var result = await _service.CleanupReportsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(50);
        result.KeptCount.Should().Be(100);
    }

    [Fact(Skip = "In-Memory DB ExecuteDelete not supported")]
    public async Task CleanupReportsAsync_ByAge_DeletesOldRecords()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = new Node { Id = nodeId, Fqdn = "node1.contoso.com" };
        _db.Nodes.Add(node);
        _db.SaveChanges();

        // 50 old reports (40 days ago)
        for (int i = 1; i <= 50; i++)
        {
            var report = new Report
            {
                Id = Guid.NewGuid(),
                NodeId = nodeId,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-40)
            };
            _db.Reports.Add(report);
        }

        // 20 recent reports (5 days ago)
        for (int i = 51; i <= 70; i++)
        {
            var report = new Report
            {
                Id = Guid.NewGuid(),
                NodeId = nodeId,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-5)
            };
            _db.Reports.Add(report);
        }

        _db.SaveChanges();

        var policy = new RecordRetentionPolicy { KeepCount = 1000, KeepDays = 30 };

        // Act
        var result = await _service.CleanupReportsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(50);
        result.KeptCount.Should().Be(20);
    }

    [Fact(Skip = "In-Memory DB ExecuteDelete not supported")]
    public async Task CleanupReportsAsync_PerNode_KeepsCountPerNode()
    {
        // Arrange
        var node1Id = Guid.NewGuid();
        var node2Id = Guid.NewGuid();
        _db.Nodes.Add(new Node { Id = node1Id, Fqdn = "node1.contoso.com" });
        _db.Nodes.Add(new Node { Id = node2Id, Fqdn = "node2.contoso.com" });
        _db.SaveChanges();

        // 100 reports for node1
        for (int i = 1; i <= 100; i++)
        {
            _db.Reports.Add(new Report
            {
                Id = Guid.NewGuid(),
                NodeId = node1Id,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-i)
            });
        }

        // 100 reports for node2
        for (int i = 1; i <= 100; i++)
        {
            _db.Reports.Add(new Report
            {
                Id = Guid.NewGuid(),
                NodeId = node2Id,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-i)
            });
        }

        _db.SaveChanges();

        var policy = new RecordRetentionPolicy { KeepCount = 50, KeepDays = 90 };

        // Act
        var result = await _service.CleanupReportsAsync(policy);

        // Assert - Should keep 50 per node = 100 total
        result.DeletedCount.Should().Be(100);
        result.KeptCount.Should().Be(100);
        _db.Reports.Count(r => r.NodeId == node1Id).Should().Be(50);
        _db.Reports.Count(r => r.NodeId == node2Id).Should().Be(50);
    }

    [Fact]
    public async Task CleanupReportsAsync_DryRun_DoesNotDelete()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = new Node { Id = nodeId, Fqdn = "node1.contoso.com" };
        _db.Nodes.Add(node);
        _db.SaveChanges();

        for (int i = 1; i <= 150; i++)
        {
            _db.Reports.Add(new Report
            {
                Id = Guid.NewGuid(),
                NodeId = nodeId,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-i)
            });
        }

        _db.SaveChanges();

        var policy = new RecordRetentionPolicy { KeepCount = 100, KeepDays = 90, DryRun = true };

        // Act
        var result = await _service.CleanupReportsAsync(policy);

        // Assert
        result.IsDryRun.Should().BeTrue();
        result.DeletedCount.Should().Be(50);
        _db.Reports.Should().HaveCount(150);
    }

    #endregion

    #region CleanupNodeStatusEventsAsync Tests

    [Fact]
    public async Task CleanupNodeStatusEventsAsync_WithEmptyDatabase_ReturnsEmptyResult()
    {
        // Arrange
        var policy = new RecordRetentionPolicy { KeepCount = 100, KeepDays = 30 };

        // Act
        var result = await _service.CleanupNodeStatusEventsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(0);
        result.KeptCount.Should().Be(0);
    }

    [Fact(Skip = "In-Memory DB Include() issue")]
    public async Task CleanupNodeStatusEventsAsync_ExceedsKeepCount_DeletesOldest()
    {
        // Arrange
        var nodeId = Guid.NewGuid();
        var node = new Node { Id = nodeId, Fqdn = "node1.contoso.com" };
        _db.Nodes.Add(node);
        _db.SaveChanges();

        // Add 150 events for the node
        for (int i = 1; i <= 150; i++)
        {
            var evt = new NodeStatusEvent
            {
                Id = i,
                NodeId = nodeId,
                LcmStatus = LcmStatus.Idle,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-i)
            };
            _db.NodeStatusEvents.Add(evt);
        }

        _db.SaveChanges();

        var policy = new RecordRetentionPolicy { KeepCount = 100, KeepDays = 90 };

        // Act
        var result = await _service.CleanupNodeStatusEventsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(50);
        result.KeptCount.Should().Be(100);
    }

    [Fact(Skip = "In-Memory DB Include() issue")]
    public async Task CleanupNodeStatusEventsAsync_PerNode_KeepsCountPerNode()
    {
        // Arrange
        var node1Id = Guid.NewGuid();
        var node2Id = Guid.NewGuid();
        _db.Nodes.Add(new Node { Id = node1Id, Fqdn = "node1.contoso.com" });
        _db.Nodes.Add(new Node { Id = node2Id, Fqdn = "node2.contoso.com" });
        _db.SaveChanges();

        // 100 events for node1
        for (int i = 1; i <= 100; i++)
        {
            _db.NodeStatusEvents.Add(new NodeStatusEvent
            {
                Id = i,
                NodeId = node1Id,
                LcmStatus = LcmStatus.Idle,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-i)
            });
        }

        // 100 events for node2
        for (int i = 101; i <= 200; i++)
        {
            _db.NodeStatusEvents.Add(new NodeStatusEvent
            {
                Id = i,
                NodeId = node2Id,
                LcmStatus = LcmStatus.Idle,
                Timestamp = DateTimeOffset.UtcNow.AddDays(-(i - 100))
            });
        }

        _db.SaveChanges();

        var policy = new RecordRetentionPolicy { KeepCount = 50, KeepDays = 90 };

        // Act
        var result = await _service.CleanupNodeStatusEventsAsync(policy);

        // Assert
        result.DeletedCount.Should().Be(100);
        result.KeptCount.Should().Be(100);
        _db.NodeStatusEvents.Count(e => e.NodeId == node1Id).Should().Be(50);
        _db.NodeStatusEvents.Count(e => e.NodeId == node2Id).Should().Be(50);
    }

    #endregion

    #region GetRunHistoryAsync Tests

    [Fact]
    public async Task GetRunHistoryAsync_WithEmptyHistory_ReturnsEmptyList()
    {
        // Act
        var result = await _service.GetRunHistoryAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRunHistoryAsync_WithMultipleRuns_ReturnsInReverseOrder()
    {
        // Arrange
        for (int i = 1; i <= 5; i++)
        {
            _db.RetentionRuns.Add(new RetentionRun
            {
                Id = Guid.NewGuid(),
                StartedAt = DateTimeOffset.UtcNow.AddHours(-i),
                CompletedAt = DateTimeOffset.UtcNow.AddHours(-i).AddMinutes(5),
                VersionType = "Configuration",
                IsScheduled = false,
                IsDryRun = false,
                DeletedCount = i,
                KeptCount = i * 10
            });
        }

        _db.SaveChanges();

        // Act
        var result = await _service.GetRunHistoryAsync();

        // Assert
        result.Should().HaveCount(5);
        result[0].DeletedCount.Should().Be(1);
        result[4].DeletedCount.Should().Be(5);
    }

    [Fact]
    public async Task GetRunHistoryAsync_WithLimit_ReturnsOnlyMostRecent()
    {
        // Arrange
        for (int i = 1; i <= 20; i++)
        {
            _db.RetentionRuns.Add(new RetentionRun
            {
                Id = Guid.NewGuid(),
                StartedAt = DateTimeOffset.UtcNow.AddHours(-i),
                CompletedAt = DateTimeOffset.UtcNow.AddHours(-i).AddMinutes(5),
                VersionType = "Configuration",
                IsScheduled = false,
                IsDryRun = false,
                DeletedCount = i,
                KeptCount = i * 10
            });
        }

        _db.SaveChanges();

        // Act
        var result = await _service.GetRunHistoryAsync(limit: 5);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetRunHistoryAsync_WithDateRangeFilter_ReturnsFiltered()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var from = now.AddDays(-7);
        var to = now.AddDays(-1);

        // Add 10 runs, some inside and some outside the range
        for (int i = 0; i < 10; i++)
        {
            _db.RetentionRuns.Add(new RetentionRun
            {
                Id = Guid.NewGuid(),
                StartedAt = from.AddDays(i),
                CompletedAt = from.AddDays(i).AddMinutes(5),
                VersionType = "Configuration",
                IsScheduled = false,
                IsDryRun = false,
                DeletedCount = i,
                KeptCount = i * 10
            });
        }

        _db.SaveChanges();

        // Act
        var result = await _service.GetRunHistoryAsync(from: from, to: to);

        // Assert
        result.Should().NotBeEmpty();
        foreach (var r in result)
        {
            (r.StartedAt >= from).Should().BeTrue();
            (r.StartedAt <= to).Should().BeTrue();
        }
    }

    #endregion

    #region Helper Methods

    private List<ConfigurationVersion> CreateConfiguration(Guid configId, string name, int versionCount)
    {
        var config = new Configuration { Id = configId, Name = name };
        _db.Configurations.Add(config);

        var versions = new List<ConfigurationVersion>();
        for (int i = 1; i <= versionCount; i++)
        {
            var version = new ConfigurationVersion
            {
                Id = Guid.NewGuid(),
                ConfigurationId = configId,
                Version = $"{i}.0.0",
                Status = ConfigurationVersionStatus.Published,
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-versionCount + i)
            };
            _db.ConfigurationVersions.Add(version);
            versions.Add(version);
        }

        _db.SaveChanges();
        return versions;
    }

    #endregion
}
