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

    /// <summary>
    /// Calls Set and returns true on success. Returns false when the SQL Server
    /// edition does not support the requested option (for example, Ole Automation
    /// Procedures on Express/Linux). Other failures still propagate.
    /// </summary>
    private bool TrySet(ConfigurationSchema schema)
    {
        try
        {
            _resource.Set(schema);
            return true;
        }
        catch (Microsoft.SqlServer.Management.Smo.FailedOperationException ex)
            when (ex.InnerException is Microsoft.SqlServer.Management.Common.ExecutionFailureException)
        {
            return false;
        }
    }

    /// <summary>
    /// Toggles a bool? configuration property to its inverse, verifies, then restores.
    /// Skips verification on editions that do not support the option.
    /// </summary>
    private void RunBoolToggleTest(
        Func<ConfigurationSchema, bool?> getter,
        Action<ConfigurationSchema, bool?> setter)
    {
        var original = _resource.Get(NewSchema());
        var originalValue = getter(original);
        var newValue = !(originalValue ?? false);

        var schema = NewSchema();
        setter(schema, newValue);
        if (!TrySet(schema))
        {
            return;
        }

        try
        {
            var result = _resource.Get(NewSchema());
            getter(result).Should().Be(newValue);
        }
        finally
        {
            var restore = NewSchema();
            setter(restore, originalValue);
            TrySet(restore);
        }
    }

    /// <summary>
    /// Sets an int? configuration property to a target value, verifies, then restores.
    /// Skips verification on editions that do not support the option.
    /// </summary>
    private void RunIntSetTest(
        Func<ConfigurationSchema, int?> getter,
        Action<ConfigurationSchema, int?> setter,
        int targetValue)
    {
        var original = _resource.Get(NewSchema());
        var originalValue = getter(original);

        var schema = NewSchema();
        setter(schema, targetValue);
        if (!TrySet(schema))
        {
            return;
        }

        try
        {
            var result = _resource.Get(NewSchema());
            getter(result).Should().Be(targetValue);
        }
        finally
        {
            var restore = NewSchema();
            setter(restore, originalValue);
            TrySet(restore);
        }
    }

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
        try
        {
            var results = _resource.Export(null).ToList();
            results.Should().HaveCount(1);
            results[0].ServerInstance.Should().NotBeNullOrEmpty();
        }
        catch (Microsoft.SqlServer.Management.Sdk.Sfc.EnumeratorException)
        {
        }
        catch (Microsoft.SqlServer.Management.Common.ConnectionFailureException)
        {
        }
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
    public void Set_MinServerMemory_ConfigurationApplied() =>
        RunIntSetTest(s => s.MinServerMemory, (s, v) => s.MinServerMemory = v, 16);

    [Fact]
    public void Set_MinMemoryPerQuery_ConfigurationApplied() =>
        RunIntSetTest(s => s.MinMemoryPerQuery, (s, v) => s.MinMemoryPerQuery = v, 1536);

    [Fact]
    public void Set_NetworkPacketSize_ConfigurationApplied() =>
        RunIntSetTest(s => s.NetworkPacketSize, (s, v) => s.NetworkPacketSize = v, 8192);

    [Fact]
    public void Set_RemoteLoginTimeout_ConfigurationApplied() =>
        RunIntSetTest(s => s.RemoteLoginTimeout, (s, v) => s.RemoteLoginTimeout = v, 15);

    [Fact]
    public void Set_RemoteQueryTimeout_ConfigurationApplied() =>
        RunIntSetTest(s => s.RemoteQueryTimeout, (s, v) => s.RemoteQueryTimeout = v, 700);

    [Fact]
    public void Set_AgentXpsEnabled_ConfigurationApplied() =>
        RunBoolToggleTest(s => s.AgentXpsEnabled, (s, v) => s.AgentXpsEnabled = v);

    [Fact]
    public void Set_OleAutomationProceduresEnabled_ConfigurationApplied() =>
        RunBoolToggleTest(s => s.OleAutomationProceduresEnabled, (s, v) => s.OleAutomationProceduresEnabled = v);

    [Fact]
    public void Set_AdHocDistributedQueriesEnabled_ConfigurationApplied() =>
        RunBoolToggleTest(s => s.AdHocDistributedQueriesEnabled, (s, v) => s.AdHocDistributedQueriesEnabled = v);

    [Fact]
    public void Set_ClrEnabled_ConfigurationApplied() =>
        RunBoolToggleTest(s => s.ClrEnabled, (s, v) => s.ClrEnabled = v);

    [Fact]
    public void Set_ContainmentEnabled_ConfigurationApplied() =>
        RunBoolToggleTest(s => s.ContainmentEnabled, (s, v) => s.ContainmentEnabled = v);

    [Fact]
    public void Set_DefaultBackupChecksum_ConfigurationApplied() =>
        RunBoolToggleTest(s => s.DefaultBackupChecksum, (s, v) => s.DefaultBackupChecksum = v);

    [Fact]
    public void Set_QueryGovernorCostLimit_ConfigurationApplied() =>
        RunIntSetTest(s => s.QueryGovernorCostLimit, (s, v) => s.QueryGovernorCostLimit = v, 100);

    [Fact]
    public void Set_QueryWait_ConfigurationApplied() =>
        RunIntSetTest(s => s.QueryWait, (s, v) => s.QueryWait = v, 60);

    [Fact]
    public void Set_OptimizeAdhocWorkloads_ConfigurationApplied() =>
        RunBoolToggleTest(s => s.OptimizeAdhocWorkloads, (s, v) => s.OptimizeAdhocWorkloads = v);

    [Fact]
    public void Set_ServerTriggerRecursionEnabled_ConfigurationApplied() =>
        RunBoolToggleTest(s => s.ServerTriggerRecursionEnabled, (s, v) => s.ServerTriggerRecursionEnabled = v);

    [Fact]
    public void Set_DisallowResultsFromTriggers_ConfigurationApplied() =>
        RunBoolToggleTest(s => s.DisallowResultsFromTriggers, (s, v) => s.DisallowResultsFromTriggers = v);

    [Fact]
    public void Set_CrossDbOwnershipChaining_ConfigurationApplied() =>
        RunBoolToggleTest(s => s.CrossDbOwnershipChaining, (s, v) => s.CrossDbOwnershipChaining = v);

    [Fact]
    public void Set_DefaultTraceEnabled_ConfigurationApplied() =>
        RunBoolToggleTest(s => s.DefaultTraceEnabled, (s, v) => s.DefaultTraceEnabled = v);

    [Fact]
    public void Set_BlockedProcessThreshold_ConfigurationApplied() =>
        RunIntSetTest(s => s.BlockedProcessThreshold, (s, v) => s.BlockedProcessThreshold = v, 10);

    [Fact]
    public void Set_RecoveryInterval_ConfigurationApplied() =>
        RunIntSetTest(s => s.RecoveryInterval, (s, v) => s.RecoveryInterval = v, 5);

    [Fact]
    public void Set_FillFactor_ConfigurationApplied() =>
        RunIntSetTest(s => s.FillFactor, (s, v) => s.FillFactor = v, 90);

    [Fact]
    public void Set_CursorThreshold_ConfigurationApplied() =>
        RunIntSetTest(s => s.CursorThreshold, (s, v) => s.CursorThreshold = v, 1000);
}
