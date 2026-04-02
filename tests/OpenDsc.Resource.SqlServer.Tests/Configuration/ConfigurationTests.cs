// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using ConfigurationResource = OpenDsc.Resource.SqlServer.Configuration.Resource;
using ConfigurationSchema = OpenDsc.Resource.SqlServer.Configuration.Schema;

using Xunit;

namespace OpenDsc.Resource.SqlServer.Tests.Configuration;

[Trait("Category", "Integration")]
public sealed class ConfigurationTests : SqlServerTestBase
{
    private readonly ConfigurationResource _resource = new(SourceGenerationContext.Default);

    public ConfigurationTests(SqlServerFixture fixture) : base(fixture) { }

    private ConfigurationSchema NewSchema() => new()
    {
        ServerInstance = ServerInstance,
        ConnectUsername = ConnectUsername,
        ConnectPassword = ConnectPassword
    };

    [Fact]
    public void GetSchema_ReturnsValidJsonSchema()
    {
        var schema = _resource.GetSchema();
        schema.Should().NotBeNullOrEmpty();
        schema.Should().Contain("serverInstance");
        schema.Should().Contain("maxServerMemory");
        schema.Should().Contain("minServerMemory");
        schema.Should().Contain("maxDegreeOfParallelism");
        schema.Should().Contain("costThresholdForParallelism");
        schema.Should().Contain("xpCmdShellEnabled");
        schema.Should().Contain("databaseMailEnabled");
        schema.Should().Contain("defaultBackupCompression");
        schema.Should().Contain("defaultBackupChecksum");
        schema.Should().Contain("networkPacketSize");
        schema.Should().Contain("remoteLoginTimeout");
        schema.Should().Contain("nestedTriggers");
        schema.Should().Contain("crossDbOwnershipChaining");
        schema.Should().Contain("remoteDacConnectionsEnabled");
        schema.Should().Contain("showAdvancedOptions");
        schema.Should().Contain("https://json-schema.org/draft/2020-12/schema");
    }

    [Fact]
    public void Get_CurrentConfiguration_ReturnsServerInstance()
    {
        var result = _resource.Get(NewSchema());
        result.Should().NotBeNull();
        result.ServerInstance.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_ReturnsMemoryValues()
    {
        var result = _resource.Get(NewSchema());
        result.MaxServerMemory.Should().NotBeNull();
        result.MaxServerMemory.Should().BeGreaterThan(0);
        result.MinServerMemory.Should().NotBeNull();
    }

    [Fact]
    public void Get_ReturnsParallelismValues()
    {
        var result = _resource.Get(NewSchema());
        result.MaxDegreeOfParallelism.Should().NotBeNull();
        result.CostThresholdForParallelism.Should().NotBeNull();
    }

    [Fact]
    public void Set_MaxDegreeOfParallelism_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalMaxDop = original.MaxDegreeOfParallelism;

        try
        {
            var schema = NewSchema();
            schema.MaxDegreeOfParallelism = 2;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.MaxDegreeOfParallelism.Should().Be(2);
        }
        finally
        {
            var restore = NewSchema();
            restore.MaxDegreeOfParallelism = originalMaxDop;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_CostThresholdForParallelism_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalCost = original.CostThresholdForParallelism;

        try
        {
            var schema = NewSchema();
            schema.CostThresholdForParallelism = 30;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.CostThresholdForParallelism.Should().Be(30);
        }
        finally
        {
            var restore = NewSchema();
            restore.CostThresholdForParallelism = originalCost;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_DefaultBackupCompression_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.DefaultBackupCompression;

        try
        {
            var schema = NewSchema();
            schema.DefaultBackupCompression = true;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.DefaultBackupCompression.Should().BeTrue();
        }
        finally
        {
            var restore = NewSchema();
            restore.DefaultBackupCompression = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_RemoteDacConnectionsEnabled_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.RemoteDacConnectionsEnabled;

        try
        {
            var schema = NewSchema();
            schema.RemoteDacConnectionsEnabled = true;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.RemoteDacConnectionsEnabled.Should().BeTrue();
        }
        finally
        {
            var restore = NewSchema();
            restore.RemoteDacConnectionsEnabled = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_MultipleProperties_AllApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalMaxDop = original.MaxDegreeOfParallelism;
        var originalCost = original.CostThresholdForParallelism;

        try
        {
            var schema = NewSchema();
            schema.MaxDegreeOfParallelism = 4;
            schema.CostThresholdForParallelism = 25;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.MaxDegreeOfParallelism.Should().Be(4);
            result.CostThresholdForParallelism.Should().Be(25);
        }
        finally
        {
            var restore = NewSchema();
            restore.MaxDegreeOfParallelism = originalMaxDop;
            restore.CostThresholdForParallelism = originalCost;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_NestedTriggers_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.NestedTriggers;

        try
        {
            var schema = NewSchema();
            schema.NestedTriggers = true;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.NestedTriggers.Should().BeTrue();
        }
        finally
        {
            var restore = NewSchema();
            restore.NestedTriggers = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Export_ReturnsConfiguration()
    {
        var filter = new ConfigurationSchema
        {
            ServerInstance = ServerInstance,
            ConnectUsername = ConnectUsername,
            ConnectPassword = ConnectPassword
        };
        var results = _resource.Export(filter).ToList();
        results.Should().HaveCount(1);
        results[0].MaxServerMemory.Should().NotBeNull();
    }
}
