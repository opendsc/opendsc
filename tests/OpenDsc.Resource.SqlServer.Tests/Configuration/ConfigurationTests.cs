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

    [Fact]
    public void Export_NullFilter_UsesDefaults()
    {
        var results = _resource.Export(null).ToList();
        results.Should().HaveCount(1);
        results[0].ServerInstance.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NullInstance_Throws()
    {
        var action = () => _resource.Get(null);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Set_NullInstance_Throws()
    {
        var action = () => _resource.Set(null);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Set_EmptyInstance_NoChangesApplied()
    {
        // No properties set — exercises the "changed=false" path (cfg.Alter not called).
        var action = () => _resource.Set(NewSchema());
        action.Should().NotThrow();
    }

    [Fact]
    public void Set_SameValueAsCurrent_NoChangeBranch()
    {
        // Setting properties to their current values exercises the "value matches" branch
        // where the assignment is skipped.
        var current = _resource.Get(NewSchema());

        var schema = NewSchema();
        schema.MaxDegreeOfParallelism = current.MaxDegreeOfParallelism;
        schema.CostThresholdForParallelism = current.CostThresholdForParallelism;
        schema.RemoteLoginTimeout = current.RemoteLoginTimeout;
        schema.RemoteQueryTimeout = current.RemoteQueryTimeout;
        schema.NetworkPacketSize = current.NetworkPacketSize;
        schema.MinServerMemory = current.MinServerMemory;
        schema.MinMemoryPerQuery = current.MinMemoryPerQuery;
        schema.FillFactor = current.FillFactor;
        schema.RecoveryInterval = current.RecoveryInterval;
        schema.CursorThreshold = current.CursorThreshold;
        schema.BlockedProcessThreshold = current.BlockedProcessThreshold;
        schema.QueryGovernorCostLimit = current.QueryGovernorCostLimit;
        schema.QueryWait = current.QueryWait;
        schema.FilestreamAccessLevel = current.FilestreamAccessLevel;
        schema.MaxWorkerThreads = current.MaxWorkerThreads;
        schema.UserConnections = current.UserConnections;
        schema.AgentXpsEnabled = current.AgentXpsEnabled;
        schema.OleAutomationProceduresEnabled = current.OleAutomationProceduresEnabled;
        schema.AdHocDistributedQueriesEnabled = current.AdHocDistributedQueriesEnabled;
        schema.ClrEnabled = current.ClrEnabled;
        schema.ContainmentEnabled = current.ContainmentEnabled;
        schema.OptimizeAdhocWorkloads = current.OptimizeAdhocWorkloads;
        schema.NestedTriggers = current.NestedTriggers;
        schema.ServerTriggerRecursionEnabled = current.ServerTriggerRecursionEnabled;
        schema.DisallowResultsFromTriggers = current.DisallowResultsFromTriggers;
        schema.CrossDbOwnershipChaining = current.CrossDbOwnershipChaining;
        schema.DefaultTraceEnabled = current.DefaultTraceEnabled;
        schema.DefaultBackupCompression = current.DefaultBackupCompression;
        schema.DefaultBackupChecksum = current.DefaultBackupChecksum;
        schema.RemoteDacConnectionsEnabled = current.RemoteDacConnectionsEnabled;
        schema.ShowAdvancedOptions = current.ShowAdvancedOptions;

        var action = () => _resource.Set(schema);
        action.Should().NotThrow();

        var after = _resource.Get(NewSchema());
        after.MaxDegreeOfParallelism.Should().Be(current.MaxDegreeOfParallelism);
    }

    [Fact]
    public void Set_MinServerMemory_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.MinServerMemory;

        try
        {
            var schema = NewSchema();
            schema.MinServerMemory = 16;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.MinServerMemory.Should().Be(16);
        }
        finally
        {
            var restore = NewSchema();
            restore.MinServerMemory = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_MinMemoryPerQuery_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.MinMemoryPerQuery;

        try
        {
            var schema = NewSchema();
            schema.MinMemoryPerQuery = 1536;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.MinMemoryPerQuery.Should().Be(1536);
        }
        finally
        {
            var restore = NewSchema();
            restore.MinMemoryPerQuery = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_NetworkPacketSize_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.NetworkPacketSize;

        try
        {
            var schema = NewSchema();
            schema.NetworkPacketSize = 8192;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.NetworkPacketSize.Should().Be(8192);
        }
        finally
        {
            var restore = NewSchema();
            restore.NetworkPacketSize = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_RemoteLoginTimeout_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.RemoteLoginTimeout;

        try
        {
            var schema = NewSchema();
            schema.RemoteLoginTimeout = 15;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.RemoteLoginTimeout.Should().Be(15);
        }
        finally
        {
            var restore = NewSchema();
            restore.RemoteLoginTimeout = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_RemoteQueryTimeout_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.RemoteQueryTimeout;

        try
        {
            var schema = NewSchema();
            schema.RemoteQueryTimeout = 700;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.RemoteQueryTimeout.Should().Be(700);
        }
        finally
        {
            var restore = NewSchema();
            restore.RemoteQueryTimeout = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_AgentXpsEnabled_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.AgentXpsEnabled;

        try
        {
            var schema = NewSchema();
            schema.AgentXpsEnabled = !(originalValue ?? false);
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.AgentXpsEnabled.Should().Be(!(originalValue ?? false));
        }
        finally
        {
            var restore = NewSchema();
            restore.AgentXpsEnabled = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_OleAutomationProceduresEnabled_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.OleAutomationProceduresEnabled;

        try
        {
            var schema = NewSchema();
            schema.OleAutomationProceduresEnabled = !(originalValue ?? false);
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.OleAutomationProceduresEnabled.Should().Be(!(originalValue ?? false));
        }
        finally
        {
            var restore = NewSchema();
            restore.OleAutomationProceduresEnabled = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_AdHocDistributedQueriesEnabled_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.AdHocDistributedQueriesEnabled;

        try
        {
            var schema = NewSchema();
            schema.AdHocDistributedQueriesEnabled = !(originalValue ?? false);
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.AdHocDistributedQueriesEnabled.Should().Be(!(originalValue ?? false));
        }
        finally
        {
            var restore = NewSchema();
            restore.AdHocDistributedQueriesEnabled = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_ClrEnabled_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.ClrEnabled;

        try
        {
            var schema = NewSchema();
            schema.ClrEnabled = !(originalValue ?? false);
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.ClrEnabled.Should().Be(!(originalValue ?? false));
        }
        finally
        {
            var restore = NewSchema();
            restore.ClrEnabled = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_ContainmentEnabled_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.ContainmentEnabled;

        try
        {
            var schema = NewSchema();
            schema.ContainmentEnabled = !(originalValue ?? false);
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.ContainmentEnabled.Should().Be(!(originalValue ?? false));
        }
        finally
        {
            var restore = NewSchema();
            restore.ContainmentEnabled = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_DefaultBackupChecksum_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.DefaultBackupChecksum;

        try
        {
            var schema = NewSchema();
            schema.DefaultBackupChecksum = !(originalValue ?? false);
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.DefaultBackupChecksum.Should().Be(!(originalValue ?? false));
        }
        finally
        {
            var restore = NewSchema();
            restore.DefaultBackupChecksum = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_QueryGovernorCostLimit_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.QueryGovernorCostLimit;

        try
        {
            var schema = NewSchema();
            schema.QueryGovernorCostLimit = 100;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.QueryGovernorCostLimit.Should().Be(100);
        }
        finally
        {
            var restore = NewSchema();
            restore.QueryGovernorCostLimit = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_QueryWait_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.QueryWait;

        try
        {
            var schema = NewSchema();
            schema.QueryWait = 60;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.QueryWait.Should().Be(60);
        }
        finally
        {
            var restore = NewSchema();
            restore.QueryWait = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_OptimizeAdhocWorkloads_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.OptimizeAdhocWorkloads;

        try
        {
            var schema = NewSchema();
            schema.OptimizeAdhocWorkloads = !(originalValue ?? false);
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.OptimizeAdhocWorkloads.Should().Be(!(originalValue ?? false));
        }
        finally
        {
            var restore = NewSchema();
            restore.OptimizeAdhocWorkloads = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_ServerTriggerRecursionEnabled_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.ServerTriggerRecursionEnabled;

        try
        {
            var schema = NewSchema();
            schema.ServerTriggerRecursionEnabled = !(originalValue ?? false);
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.ServerTriggerRecursionEnabled.Should().Be(!(originalValue ?? false));
        }
        finally
        {
            var restore = NewSchema();
            restore.ServerTriggerRecursionEnabled = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_DisallowResultsFromTriggers_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.DisallowResultsFromTriggers;

        try
        {
            var schema = NewSchema();
            schema.DisallowResultsFromTriggers = !(originalValue ?? false);
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.DisallowResultsFromTriggers.Should().Be(!(originalValue ?? false));
        }
        finally
        {
            var restore = NewSchema();
            restore.DisallowResultsFromTriggers = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_CrossDbOwnershipChaining_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.CrossDbOwnershipChaining;

        try
        {
            var schema = NewSchema();
            schema.CrossDbOwnershipChaining = !(originalValue ?? false);
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.CrossDbOwnershipChaining.Should().Be(!(originalValue ?? false));
        }
        finally
        {
            var restore = NewSchema();
            restore.CrossDbOwnershipChaining = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_DefaultTraceEnabled_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.DefaultTraceEnabled;

        try
        {
            var schema = NewSchema();
            schema.DefaultTraceEnabled = !(originalValue ?? false);
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.DefaultTraceEnabled.Should().Be(!(originalValue ?? false));
        }
        finally
        {
            var restore = NewSchema();
            restore.DefaultTraceEnabled = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_BlockedProcessThreshold_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.BlockedProcessThreshold;

        try
        {
            var schema = NewSchema();
            schema.BlockedProcessThreshold = 10;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.BlockedProcessThreshold.Should().Be(10);
        }
        finally
        {
            var restore = NewSchema();
            restore.BlockedProcessThreshold = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_RecoveryInterval_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.RecoveryInterval;

        try
        {
            var schema = NewSchema();
            schema.RecoveryInterval = 5;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.RecoveryInterval.Should().Be(5);
        }
        finally
        {
            var restore = NewSchema();
            restore.RecoveryInterval = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_FillFactor_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.FillFactor;

        try
        {
            var schema = NewSchema();
            schema.FillFactor = 90;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.FillFactor.Should().Be(90);
        }
        finally
        {
            var restore = NewSchema();
            restore.FillFactor = originalValue;
            _resource.Set(restore);
        }
    }

    [Fact]
    public void Set_CursorThreshold_ConfigurationApplied()
    {
        var original = _resource.Get(NewSchema());
        var originalValue = original.CursorThreshold;

        try
        {
            var schema = NewSchema();
            schema.CursorThreshold = 1000;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema());
            result.CursorThreshold.Should().Be(1000);
        }
        finally
        {
            var restore = NewSchema();
            restore.CursorThreshold = originalValue;
            _resource.Set(restore);
        }
    }
}
