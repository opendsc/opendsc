// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer.Configuration;

/// <summary>
/// DSC resource for managing SQL Server instance configuration options.
/// </summary>
[DscResource("OpenDsc.SqlServer/Configuration", "0.1.0",
    Description = "Manage SQL Server instance configuration options",
    Tags = ["sql", "sqlserver", "configuration", "sp_configure"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[ExitCode(4, Exception = typeof(UnauthorizedAccessException), Description = "Unauthorized access")]
[ExitCode(5, Exception = typeof(InvalidOperationException), Description = "Invalid operation")]
public sealed class Resource(JsonSerializerContext context)
    : DscResource<Schema>(context),
      IGettable<Schema>,
      ISettable<Schema>,
      IExportable<Schema>
{
    public override string GetSchema()
    {
        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase
        };

        var builder = new JsonSchemaBuilder().FromType<Schema>(config);
        builder.Schema("https://json-schema.org/draft/2020-12/schema");
        var schema = builder.Build();

        return JsonSerializer.Serialize(schema);
    }

    public Schema Get(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var server = SqlConnectionHelper.CreateConnection(
            instance.ServerInstance,
            instance.ConnectUsername,
            instance.ConnectPassword);

        try
        {
            var cfg = server.Configuration;

            return new Schema
            {
                ServerInstance = instance.ServerInstance,

                // Memory
                MaxServerMemory = cfg.MaxServerMemory.ConfigValue,
                MinServerMemory = cfg.MinServerMemory.ConfigValue,
                MinMemoryPerQuery = cfg.MinMemoryPerQuery.ConfigValue,

                // Parallelism
                MaxDegreeOfParallelism = cfg.MaxDegreeOfParallelism.ConfigValue,
                CostThresholdForParallelism = cfg.CostThresholdForParallelism.ConfigValue,

                // Network
                NetworkPacketSize = cfg.NetworkPacketSize.ConfigValue,
                RemoteLoginTimeout = cfg.RemoteLoginTimeout.ConfigValue,
                RemoteQueryTimeout = cfg.RemoteQueryTimeout.ConfigValue,

                // Feature toggles
                XpCmdShellEnabled = cfg.XPCmdShellEnabled.ConfigValue == 1,
                DatabaseMailEnabled = cfg.DatabaseMailEnabled.ConfigValue == 1,
                AgentXpsEnabled = cfg.AgentXPsEnabled.ConfigValue == 1,
                OleAutomationProceduresEnabled = cfg.OleAutomationProceduresEnabled.ConfigValue == 1,
                AdHocDistributedQueriesEnabled = cfg.AdHocDistributedQueriesEnabled.ConfigValue == 1,
                ClrEnabled = cfg.IsSqlClrEnabled.ConfigValue == 1,
                RemoteDacConnectionsEnabled = cfg.RemoteDacConnectionsEnabled.ConfigValue == 1,
                ContainmentEnabled = cfg.ContainmentEnabled.ConfigValue == 1,

                // Backup
                DefaultBackupCompression = cfg.DefaultBackupCompression.ConfigValue == 1,
                DefaultBackupChecksum = cfg.DefaultBackupChecksum.ConfigValue == 1,

                // Query
                QueryGovernorCostLimit = cfg.QueryGovernorCostLimit.ConfigValue,
                QueryWait = cfg.QueryWait.ConfigValue,
                OptimizeAdhocWorkloads = cfg.OptimizeAdhocWorkloads.ConfigValue == 1,

                // Triggers
                NestedTriggers = cfg.NestedTriggers.ConfigValue == 1,
                ServerTriggerRecursionEnabled = cfg.ServerTriggerRecursionEnabled.ConfigValue == 1,
                DisallowResultsFromTriggers = cfg.DisallowResultsFromTriggers.ConfigValue == 1,

                // Security
                C2AuditMode = cfg.C2AuditMode.ConfigValue == 1,
                CommonCriteriaComplianceEnabled = cfg.CommonCriteriaComplianceEnabled.ConfigValue == 1,
                CrossDbOwnershipChaining = cfg.CrossDBOwnershipChaining.ConfigValue == 1,

                // Misc
                DefaultTraceEnabled = cfg.DefaultTraceEnabled.ConfigValue == 1,
                BlockedProcessThreshold = cfg.BlockedProcessThreshold.ConfigValue,
                ShowAdvancedOptions = cfg.ShowAdvancedOptions.ConfigValue == 1,
                RecoveryInterval = cfg.RecoveryInterval.ConfigValue,
                FillFactor = cfg.FillFactor.ConfigValue,
                UserConnections = cfg.UserConnections.ConfigValue,
                CursorThreshold = cfg.CursorThreshold.ConfigValue,
                FilestreamAccessLevel = cfg.FilestreamAccessLevel.ConfigValue,
                MaxWorkerThreads = cfg.MaxWorkerThreads.ConfigValue,

                // Read-only
                ShowAdvancedOptionsRunValue = cfg.ShowAdvancedOptions.RunValue == 1
            };
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var server = SqlConnectionHelper.CreateConnection(
            instance.ServerInstance,
            instance.ConnectUsername,
            instance.ConnectPassword);

        try
        {
            var cfg = server.Configuration;
            bool changed = false;

            // Memory configuration
            if (instance.MaxServerMemory.HasValue && cfg.MaxServerMemory.ConfigValue != instance.MaxServerMemory.Value)
            {
                cfg.MaxServerMemory.ConfigValue = instance.MaxServerMemory.Value;
                changed = true;
            }

            if (instance.MinServerMemory.HasValue && cfg.MinServerMemory.ConfigValue != instance.MinServerMemory.Value)
            {
                cfg.MinServerMemory.ConfigValue = instance.MinServerMemory.Value;
                changed = true;
            }

            if (instance.MinMemoryPerQuery.HasValue && cfg.MinMemoryPerQuery.ConfigValue != instance.MinMemoryPerQuery.Value)
            {
                cfg.MinMemoryPerQuery.ConfigValue = instance.MinMemoryPerQuery.Value;
                changed = true;
            }

            // Parallelism configuration
            if (instance.MaxDegreeOfParallelism.HasValue && cfg.MaxDegreeOfParallelism.ConfigValue != instance.MaxDegreeOfParallelism.Value)
            {
                cfg.MaxDegreeOfParallelism.ConfigValue = instance.MaxDegreeOfParallelism.Value;
                changed = true;
            }

            if (instance.CostThresholdForParallelism.HasValue && cfg.CostThresholdForParallelism.ConfigValue != instance.CostThresholdForParallelism.Value)
            {
                cfg.CostThresholdForParallelism.ConfigValue = instance.CostThresholdForParallelism.Value;
                changed = true;
            }

            // Network configuration
            if (instance.NetworkPacketSize.HasValue && cfg.NetworkPacketSize.ConfigValue != instance.NetworkPacketSize.Value)
            {
                cfg.NetworkPacketSize.ConfigValue = instance.NetworkPacketSize.Value;
                changed = true;
            }

            if (instance.RemoteLoginTimeout.HasValue && cfg.RemoteLoginTimeout.ConfigValue != instance.RemoteLoginTimeout.Value)
            {
                cfg.RemoteLoginTimeout.ConfigValue = instance.RemoteLoginTimeout.Value;
                changed = true;
            }

            if (instance.RemoteQueryTimeout.HasValue && cfg.RemoteQueryTimeout.ConfigValue != instance.RemoteQueryTimeout.Value)
            {
                cfg.RemoteQueryTimeout.ConfigValue = instance.RemoteQueryTimeout.Value;
                changed = true;
            }

            // Feature toggles
            if (instance.XpCmdShellEnabled.HasValue)
            {
                int desiredValue = instance.XpCmdShellEnabled.Value ? 1 : 0;
                if (cfg.XPCmdShellEnabled.ConfigValue != desiredValue)
                {
                    cfg.XPCmdShellEnabled.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.DatabaseMailEnabled.HasValue)
            {
                int desiredValue = instance.DatabaseMailEnabled.Value ? 1 : 0;
                if (cfg.DatabaseMailEnabled.ConfigValue != desiredValue)
                {
                    cfg.DatabaseMailEnabled.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.AgentXpsEnabled.HasValue)
            {
                int desiredValue = instance.AgentXpsEnabled.Value ? 1 : 0;
                if (cfg.AgentXPsEnabled.ConfigValue != desiredValue)
                {
                    cfg.AgentXPsEnabled.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.OleAutomationProceduresEnabled.HasValue)
            {
                int desiredValue = instance.OleAutomationProceduresEnabled.Value ? 1 : 0;
                if (cfg.OleAutomationProceduresEnabled.ConfigValue != desiredValue)
                {
                    cfg.OleAutomationProceduresEnabled.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.AdHocDistributedQueriesEnabled.HasValue)
            {
                int desiredValue = instance.AdHocDistributedQueriesEnabled.Value ? 1 : 0;
                if (cfg.AdHocDistributedQueriesEnabled.ConfigValue != desiredValue)
                {
                    cfg.AdHocDistributedQueriesEnabled.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.ClrEnabled.HasValue)
            {
                int desiredValue = instance.ClrEnabled.Value ? 1 : 0;
                if (cfg.IsSqlClrEnabled.ConfigValue != desiredValue)
                {
                    cfg.IsSqlClrEnabled.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.RemoteDacConnectionsEnabled.HasValue)
            {
                int desiredValue = instance.RemoteDacConnectionsEnabled.Value ? 1 : 0;
                if (cfg.RemoteDacConnectionsEnabled.ConfigValue != desiredValue)
                {
                    cfg.RemoteDacConnectionsEnabled.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.ContainmentEnabled.HasValue)
            {
                int desiredValue = instance.ContainmentEnabled.Value ? 1 : 0;
                if (cfg.ContainmentEnabled.ConfigValue != desiredValue)
                {
                    cfg.ContainmentEnabled.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            // Backup configuration
            if (instance.DefaultBackupCompression.HasValue)
            {
                int desiredValue = instance.DefaultBackupCompression.Value ? 1 : 0;
                if (cfg.DefaultBackupCompression.ConfigValue != desiredValue)
                {
                    cfg.DefaultBackupCompression.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.DefaultBackupChecksum.HasValue)
            {
                int desiredValue = instance.DefaultBackupChecksum.Value ? 1 : 0;
                if (cfg.DefaultBackupChecksum.ConfigValue != desiredValue)
                {
                    cfg.DefaultBackupChecksum.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            // Query configuration
            if (instance.QueryGovernorCostLimit.HasValue && cfg.QueryGovernorCostLimit.ConfigValue != instance.QueryGovernorCostLimit.Value)
            {
                cfg.QueryGovernorCostLimit.ConfigValue = instance.QueryGovernorCostLimit.Value;
                changed = true;
            }

            if (instance.QueryWait.HasValue && cfg.QueryWait.ConfigValue != instance.QueryWait.Value)
            {
                cfg.QueryWait.ConfigValue = instance.QueryWait.Value;
                changed = true;
            }

            if (instance.OptimizeAdhocWorkloads.HasValue)
            {
                int desiredValue = instance.OptimizeAdhocWorkloads.Value ? 1 : 0;
                if (cfg.OptimizeAdhocWorkloads.ConfigValue != desiredValue)
                {
                    cfg.OptimizeAdhocWorkloads.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            // Trigger configuration
            if (instance.NestedTriggers.HasValue)
            {
                int desiredValue = instance.NestedTriggers.Value ? 1 : 0;
                if (cfg.NestedTriggers.ConfigValue != desiredValue)
                {
                    cfg.NestedTriggers.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.ServerTriggerRecursionEnabled.HasValue)
            {
                int desiredValue = instance.ServerTriggerRecursionEnabled.Value ? 1 : 0;
                if (cfg.ServerTriggerRecursionEnabled.ConfigValue != desiredValue)
                {
                    cfg.ServerTriggerRecursionEnabled.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.DisallowResultsFromTriggers.HasValue)
            {
                int desiredValue = instance.DisallowResultsFromTriggers.Value ? 1 : 0;
                if (cfg.DisallowResultsFromTriggers.ConfigValue != desiredValue)
                {
                    cfg.DisallowResultsFromTriggers.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            // Security configuration
            if (instance.C2AuditMode.HasValue)
            {
                int desiredValue = instance.C2AuditMode.Value ? 1 : 0;
                if (cfg.C2AuditMode.ConfigValue != desiredValue)
                {
                    cfg.C2AuditMode.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.CommonCriteriaComplianceEnabled.HasValue)
            {
                int desiredValue = instance.CommonCriteriaComplianceEnabled.Value ? 1 : 0;
                if (cfg.CommonCriteriaComplianceEnabled.ConfigValue != desiredValue)
                {
                    cfg.CommonCriteriaComplianceEnabled.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.CrossDbOwnershipChaining.HasValue)
            {
                int desiredValue = instance.CrossDbOwnershipChaining.Value ? 1 : 0;
                if (cfg.CrossDBOwnershipChaining.ConfigValue != desiredValue)
                {
                    cfg.CrossDBOwnershipChaining.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            // Misc configuration
            if (instance.DefaultTraceEnabled.HasValue)
            {
                int desiredValue = instance.DefaultTraceEnabled.Value ? 1 : 0;
                if (cfg.DefaultTraceEnabled.ConfigValue != desiredValue)
                {
                    cfg.DefaultTraceEnabled.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.BlockedProcessThreshold.HasValue && cfg.BlockedProcessThreshold.ConfigValue != instance.BlockedProcessThreshold.Value)
            {
                cfg.BlockedProcessThreshold.ConfigValue = instance.BlockedProcessThreshold.Value;
                changed = true;
            }

            if (instance.ShowAdvancedOptions.HasValue)
            {
                int desiredValue = instance.ShowAdvancedOptions.Value ? 1 : 0;
                if (cfg.ShowAdvancedOptions.ConfigValue != desiredValue)
                {
                    cfg.ShowAdvancedOptions.ConfigValue = desiredValue;
                    changed = true;
                }
            }

            if (instance.RecoveryInterval.HasValue && cfg.RecoveryInterval.ConfigValue != instance.RecoveryInterval.Value)
            {
                cfg.RecoveryInterval.ConfigValue = instance.RecoveryInterval.Value;
                changed = true;
            }

            if (instance.FillFactor.HasValue && cfg.FillFactor.ConfigValue != instance.FillFactor.Value)
            {
                cfg.FillFactor.ConfigValue = instance.FillFactor.Value;
                changed = true;
            }

            if (instance.UserConnections.HasValue && cfg.UserConnections.ConfigValue != instance.UserConnections.Value)
            {
                cfg.UserConnections.ConfigValue = instance.UserConnections.Value;
                changed = true;
            }

            if (instance.CursorThreshold.HasValue && cfg.CursorThreshold.ConfigValue != instance.CursorThreshold.Value)
            {
                cfg.CursorThreshold.ConfigValue = instance.CursorThreshold.Value;
                changed = true;
            }

            if (instance.FilestreamAccessLevel.HasValue && cfg.FilestreamAccessLevel.ConfigValue != instance.FilestreamAccessLevel.Value)
            {
                cfg.FilestreamAccessLevel.ConfigValue = instance.FilestreamAccessLevel.Value;
                changed = true;
            }

            if (instance.MaxWorkerThreads.HasValue && cfg.MaxWorkerThreads.ConfigValue != instance.MaxWorkerThreads.Value)
            {
                cfg.MaxWorkerThreads.ConfigValue = instance.MaxWorkerThreads.Value;
                changed = true;
            }

            if (changed)
            {
                cfg.Alter();
            }

            return null;
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    public IEnumerable<Schema> Export(Schema? filter)
    {
        var serverInstance = filter?.ServerInstance ?? ".";
        var username = filter?.ConnectUsername;
        var password = filter?.ConnectPassword;

        var instance = new Schema
        {
            ServerInstance = serverInstance,
            ConnectUsername = username,
            ConnectPassword = password
        };

        yield return Get(instance);
    }
}
