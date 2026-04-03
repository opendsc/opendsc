// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

using Microsoft.SqlServer.Management.Smo;

namespace OpenDsc.Resource.SqlServer.AvailabilityGroup;

[Title("SQL Server Availability Group Schema")]
[Description("Schema for managing SQL Server Always On Availability Groups via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The name of the SQL Server instance to connect to. Use '.' or '(local)' for the default local instance, or 'servername\\instancename' for named instances.")]
    [Pattern(@"^.+$")]
    public string ServerInstance { get; set; } = string.Empty;

    [Description("The username for SQL Server authentication when connecting to the server. If not specified, Windows Authentication is used.")]
    [Nullable(false)]
    [WriteOnly]
    public string? ConnectUsername { get; set; }

    [Description("The password for SQL Server authentication when connecting to the server. Required when ConnectUsername is specified.")]
    [Nullable(false)]
    [WriteOnly]
    public string? ConnectPassword { get; set; }

    [Required]
    [Description("The name of the availability group.")]
    [Pattern(@"^.+$")]
    public string Name { get; set; } = string.Empty;

    [Description("The automated backup preference for the availability group.")]
    [Nullable(false)]
    public AvailabilityGroupAutomatedBackupPreference? AutomatedBackupPreference { get; set; }

    [Description("The failure condition level that triggers an automatic failover.")]
    [Nullable(false)]
    public AvailabilityGroupFailureConditionLevel? FailureConditionLevel { get; set; }

    [Description("The health check timeout value in milliseconds.")]
    [Nullable(false)]
    public int? HealthCheckTimeout { get; set; }

    [Description("Whether this is a basic availability group (limited to two replicas and one database).")]
    [Nullable(false)]
    public bool? BasicAvailabilityGroup { get; set; }

    [Description("Whether the availability group supports database-level health detection.")]
    [Nullable(false)]
    public bool? DatabaseHealthTrigger { get; set; }

    [Description("Whether DTC support is enabled for the availability group.")]
    [Nullable(false)]
    public bool? DtcSupportEnabled { get; set; }

    [Description("Whether the availability group is a distributed availability group.")]
    [Nullable(false)]
    public bool? IsDistributedAvailabilityGroup { get; set; }

    [Description("The cluster type of the availability group.")]
    [Nullable(false)]
    public AvailabilityGroupClusterType? ClusterType { get; set; }

    [Description("The number of required synchronized secondaries to commit.")]
    [Nullable(false)]
    public int? RequiredSynchronizedSecondariesToCommit { get; set; }

    [Description("Whether the availability group is contained.")]
    [Nullable(false)]
    public bool? IsContained { get; set; }

    [ReadOnly]
    [Description("The databases participating in the availability group.")]
    public string[]? Databases { get; set; }

    [ReadOnly]
    [Description("The name of the server that is the current primary replica.")]
    public string? PrimaryReplicaServerName { get; set; }

    [ReadOnly]
    [Description("The role of the local replica in the availability group.")]
    public AvailabilityReplicaRole? LocalReplicaRole { get; set; }

    [ReadOnly]
    [Description("The unique identifier of the availability group.")]
    public Guid? UniqueId { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the availability group exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
