// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using Microsoft.SqlServer.Management.Smo;

using SmoAvailabilityGroup = Microsoft.SqlServer.Management.Smo.AvailabilityGroup;

namespace OpenDsc.Resource.SqlServer.AvailabilityGroup;

[DscResource("OpenDsc.SqlServer/AvailabilityGroup", "0.1.0", Description = "Manage SQL Server Always On Availability Groups", Tags = ["sql", "sqlserver", "availability", "group", "hadr"])]
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
      IDeletable<Schema>,
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

        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var ag = server.AvailabilityGroups.Cast<SmoAvailabilityGroup>()
                .FirstOrDefault(a => string.Equals(a.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (ag == null)
            {
                return new Schema
                {
                    ServerInstance = instance.ServerInstance,
                    Name = instance.Name,
                    Exist = false
                };
            }

            return MapToSchema(instance.ServerInstance, ag);
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

        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var ag = server.AvailabilityGroups.Cast<SmoAvailabilityGroup>()
                .FirstOrDefault(a => string.Equals(a.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (ag == null)
            {
                CreateAvailabilityGroup(server, instance);
            }
            else
            {
                UpdateAvailabilityGroup(ag, instance);
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

    public void Delete(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var ag = server.AvailabilityGroups.Cast<SmoAvailabilityGroup>()
                .FirstOrDefault(a => string.Equals(a.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            ag?.Drop();
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

        var server = SqlConnectionHelper.CreateConnection(serverInstance, username, password);

        try
        {
            foreach (SmoAvailabilityGroup ag in server.AvailabilityGroups)
            {
                yield return MapToSchema(serverInstance, ag);
            }
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    private static void CreateAvailabilityGroup(Server server, Schema instance)
    {
        var ag = new SmoAvailabilityGroup(server, instance.Name);

        if (instance.AutomatedBackupPreference.HasValue)
        {
            ag.AutomatedBackupPreference = instance.AutomatedBackupPreference.Value;
        }

        if (instance.FailureConditionLevel.HasValue)
        {
            ag.FailureConditionLevel = instance.FailureConditionLevel.Value;
        }

        if (instance.HealthCheckTimeout.HasValue)
        {
            ag.HealthCheckTimeout = instance.HealthCheckTimeout.Value;
        }

        if (instance.BasicAvailabilityGroup.HasValue)
        {
            ag.BasicAvailabilityGroup = instance.BasicAvailabilityGroup.Value;
        }

        if (instance.DatabaseHealthTrigger.HasValue)
        {
            ag.DatabaseHealthTrigger = instance.DatabaseHealthTrigger.Value;
        }

        if (instance.DtcSupportEnabled.HasValue)
        {
            ag.DtcSupportEnabled = instance.DtcSupportEnabled.Value;
        }

        if (instance.ClusterType.HasValue)
        {
            ag.ClusterType = instance.ClusterType.Value;
        }

        if (instance.RequiredSynchronizedSecondariesToCommit.HasValue)
        {
            ag.RequiredSynchronizedSecondariesToCommit = instance.RequiredSynchronizedSecondariesToCommit.Value;
        }

        if (instance.IsContained.HasValue)
        {
            ag.IsContained = instance.IsContained.Value;
        }

        ag.Create();
    }

    private static void UpdateAvailabilityGroup(SmoAvailabilityGroup ag, Schema instance)
    {
        bool changed = false;

        if (instance.AutomatedBackupPreference.HasValue && ag.AutomatedBackupPreference != instance.AutomatedBackupPreference.Value)
        {
            ag.AutomatedBackupPreference = instance.AutomatedBackupPreference.Value;
            changed = true;
        }

        if (instance.FailureConditionLevel.HasValue && ag.FailureConditionLevel != instance.FailureConditionLevel.Value)
        {
            ag.FailureConditionLevel = instance.FailureConditionLevel.Value;
            changed = true;
        }

        if (instance.HealthCheckTimeout.HasValue && ag.HealthCheckTimeout != instance.HealthCheckTimeout.Value)
        {
            ag.HealthCheckTimeout = instance.HealthCheckTimeout.Value;
            changed = true;
        }

        if (instance.DatabaseHealthTrigger.HasValue && ag.DatabaseHealthTrigger != instance.DatabaseHealthTrigger.Value)
        {
            ag.DatabaseHealthTrigger = instance.DatabaseHealthTrigger.Value;
            changed = true;
        }

        if (instance.DtcSupportEnabled.HasValue && ag.DtcSupportEnabled != instance.DtcSupportEnabled.Value)
        {
            ag.DtcSupportEnabled = instance.DtcSupportEnabled.Value;
            changed = true;
        }

        if (instance.RequiredSynchronizedSecondariesToCommit.HasValue && ag.RequiredSynchronizedSecondariesToCommit != instance.RequiredSynchronizedSecondariesToCommit.Value)
        {
            ag.RequiredSynchronizedSecondariesToCommit = instance.RequiredSynchronizedSecondariesToCommit.Value;
            changed = true;
        }

        if (changed)
        {
            ag.Alter();
        }
    }

    private static Schema MapToSchema(string serverInstance, SmoAvailabilityGroup ag)
    {
        var databases = ag.AvailabilityDatabases.Cast<AvailabilityDatabase>()
            .Select(d => d.Name)
            .ToArray();

        return new Schema
        {
            ServerInstance = serverInstance,
            Name = ag.Name,
            AutomatedBackupPreference = ag.AutomatedBackupPreference,
            FailureConditionLevel = ag.FailureConditionLevel,
            HealthCheckTimeout = ag.HealthCheckTimeout,
            BasicAvailabilityGroup = ag.BasicAvailabilityGroup,
            DatabaseHealthTrigger = ag.DatabaseHealthTrigger,
            DtcSupportEnabled = ag.DtcSupportEnabled,
            IsDistributedAvailabilityGroup = ag.IsDistributedAvailabilityGroup,
            ClusterType = ag.ClusterType,
            RequiredSynchronizedSecondariesToCommit = ag.RequiredSynchronizedSecondariesToCommit,
            IsContained = ag.IsContained,
            Databases = databases.Length > 0 ? databases : null,
            PrimaryReplicaServerName = ag.PrimaryReplicaServerName,
            LocalReplicaRole = ag.LocalReplicaRole,
            UniqueId = ag.UniqueId
        };
    }
}
