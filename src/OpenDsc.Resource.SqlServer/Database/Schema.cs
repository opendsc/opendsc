// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

using Microsoft.SqlServer.Management.Smo;

namespace OpenDsc.Resource.SqlServer.Database;

[Title("SQL Server Database Schema")]
[Description("Schema for managing SQL Server databases via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The name of the SQL Server instance to connect to. Use '.' or '(local)' for the default local instance, or 'servername\\instancename' for named instances.")]
    [Pattern(@"^.+$")]
    public string ServerInstance { get; set; } = string.Empty;

    [Description("Authentication settings for connecting to SQL Server. If not specified, Windows Authentication is used.")]
    [Nullable(false)]
    public SqlAuthentication? Authentication { get; set; }

    [Required]
    [Description("The name of the database.")]
    [Pattern(@"^[^\[\]]+$")]
    public string Name { get; set; } = string.Empty;

    // File configuration
    [Description("The path to the primary data file (.mdf). Only used when creating a database.")]
    [Nullable(false)]
    [WriteOnly]
    public string? PrimaryFilePath { get; set; }

    [Description("The path to the log file (.ldf). Only used when creating a database.")]
    [Nullable(false)]
    [WriteOnly]
    public string? LogFilePath { get; set; }

    [Description("The initial size of the primary data file in MB. Only used when creating a database.")]
    [Nullable(false)]
    [WriteOnly]
    public int? PrimaryFileSize { get; set; }

    [Description("The initial size of the log file in MB. Only used when creating a database.")]
    [Nullable(false)]
    [WriteOnly]
    public int? LogFileSize { get; set; }

    [Description("The file growth amount in MB for the primary data file. Only used when creating a database.")]
    [Nullable(false)]
    [WriteOnly]
    public int? PrimaryFileGrowth { get; set; }

    [Description("The file growth amount in MB for the log file. Only used when creating a database.")]
    [Nullable(false)]
    [WriteOnly]
    public int? LogFileGrowth { get; set; }

    // Database options
    [Description("The database collation. If not specified, the server default collation is used.")]
    [Nullable(false)]
    public string? Collation { get; set; }

    [Description("The compatibility level of the database.")]
    [Nullable(false)]
    public CompatibilityLevel? CompatibilityLevel { get; set; }

    [Description("The recovery model of the database.")]
    [Nullable(false)]
    public RecoveryModel? RecoveryModel { get; set; }

    [Description("The login name of the database owner.")]
    [Nullable(false)]
    public string? Owner { get; set; }

    // Access and state options
    [Description("Whether the database is read-only.")]
    [Nullable(false)]
    public bool? ReadOnly { get; set; }

    [Description("The user access mode of the database.")]
    [Nullable(false)]
    public DatabaseUserAccess? UserAccess { get; set; }

    [Description("The page verification method.")]
    [Nullable(false)]
    public PageVerify? PageVerify { get; set; }

    [Description("The containment type of the database.")]
    [Nullable(false)]
    public ContainmentType? ContainmentType { get; set; }

    // ANSI options
    [Description("Whether ANSI NULL default is enabled.")]
    [Nullable(false)]
    public bool? AnsiNullDefault { get; set; }

    [Description("Whether ANSI NULLs are enabled.")]
    [Nullable(false)]
    public bool? AnsiNullsEnabled { get; set; }

    [Description("Whether ANSI padding is enabled.")]
    [Nullable(false)]
    public bool? AnsiPaddingEnabled { get; set; }

    [Description("Whether ANSI warnings are enabled.")]
    [Nullable(false)]
    public bool? AnsiWarningsEnabled { get; set; }

    [Description("Whether arithmetic abort is enabled.")]
    [Nullable(false)]
    public bool? ArithmeticAbortEnabled { get; set; }

    [Description("Whether concatenating null yields null.")]
    [Nullable(false)]
    public bool? ConcatenateNullYieldsNull { get; set; }

    [Description("Whether numeric round-abort is enabled.")]
    [Nullable(false)]
    public bool? NumericRoundAbortEnabled { get; set; }

    [Description("Whether quoted identifiers are enabled.")]
    [Nullable(false)]
    public bool? QuotedIdentifiersEnabled { get; set; }

    // Auto options
    [Description("Whether the database is automatically closed when the last user exits.")]
    [Nullable(false)]
    public bool? AutoClose { get; set; }

    [Description("Whether the database is automatically shrunk.")]
    [Nullable(false)]
    public bool? AutoShrink { get; set; }

    [Description("Whether automatic statistics creation is enabled.")]
    [Nullable(false)]
    public bool? AutoCreateStatisticsEnabled { get; set; }

    [Description("Whether automatic statistics update is enabled.")]
    [Nullable(false)]
    public bool? AutoUpdateStatisticsEnabled { get; set; }

    [Description("Whether automatic statistics update runs asynchronously.")]
    [Nullable(false)]
    public bool? AutoUpdateStatisticsAsync { get; set; }

    // Cursor options
    [Description("Whether cursors are closed when a transaction commits.")]
    [Nullable(false)]
    public bool? CloseCursorsOnCommitEnabled { get; set; }

    [Description("Whether cursors default to local scope.")]
    [Nullable(false)]
    public bool? LocalCursorsDefault { get; set; }

    // Trigger options
    [Description("Whether nested triggers are enabled.")]
    [Nullable(false)]
    public bool? NestedTriggersEnabled { get; set; }

    [Description("Whether recursive triggers are enabled.")]
    [Nullable(false)]
    public bool? RecursiveTriggersEnabled { get; set; }

    // Advanced options
    [Description("Whether the database is trustworthy.")]
    [Nullable(false)]
    public bool? Trustworthy { get; set; }

    [Description("Whether database ownership chaining is enabled.")]
    [Nullable(false)]
    public bool? DatabaseOwnershipChaining { get; set; }

    [Description("Whether date correlation optimization is enabled.")]
    [Nullable(false)]
    public bool? DateCorrelationOptimization { get; set; }

    [Description("Whether the broker is enabled.")]
    [Nullable(false)]
    public bool? BrokerEnabled { get; set; }

    [Description("Whether encryption is enabled on the database.")]
    [Nullable(false)]
    public bool? EncryptionEnabled { get; set; }

    [Description("Whether forced parameterization is enabled.")]
    [Nullable(false)]
    public bool? IsParameterizationForced { get; set; }

    [Description("Whether read committed snapshot isolation is enabled.")]
    [Nullable(false)]
    public bool? IsReadCommittedSnapshotOn { get; set; }

    [Description("Whether full-text indexing is enabled.")]
    [Nullable(false)]
    public bool? IsFullTextEnabled { get; set; }

    [Description("The target recovery time in seconds.")]
    [Nullable(false)]
    public int? TargetRecoveryTime { get; set; }

    [Description("Whether delayed durability is enabled.")]
    [Nullable(false)]
    public bool? DelayedDurabilityEnabled { get; set; }

    [Description("Whether accelerated database recovery is enabled.")]
    [Nullable(false)]
    public bool? AcceleratedRecoveryEnabled { get; set; }

    // Read-only properties
    [ReadOnly]
    [Description("The database ID.")]
    public int? Id { get; set; }

    [ReadOnly]
    [Description("The creation date of the database.")]
    public DateTime? CreateDate { get; set; }

    [ReadOnly]
    [Description("The current size of the database in MB.")]
    public double? Size { get; set; }

    [ReadOnly]
    [Description("The space available in the database in KB.")]
    public double? SpaceAvailable { get; set; }

    [ReadOnly]
    [Description("The data space usage in KB.")]
    public double? DataSpaceUsage { get; set; }

    [ReadOnly]
    [Description("The index space usage in KB.")]
    public double? IndexSpaceUsage { get; set; }

    [ReadOnly]
    [Description("The number of active connections to the database.")]
    public int? ActiveConnections { get; set; }

    [ReadOnly]
    [Description("The date of the last full backup.")]
    public DateTime? LastBackupDate { get; set; }

    [ReadOnly]
    [Description("The date of the last differential backup.")]
    public DateTime? LastDifferentialBackupDate { get; set; }

    [ReadOnly]
    [Description("The date of the last log backup.")]
    public DateTime? LastLogBackupDate { get; set; }

    [ReadOnly]
    [Description("The status of the database.")]
    public string? Status { get; set; }

    [ReadOnly]
    [Description("Whether the database is a system database.")]
    public bool? IsSystemObject { get; set; }

    [ReadOnly]
    [Description("Whether the database is accessible.")]
    public bool? IsAccessible { get; set; }

    [ReadOnly]
    [Description("Whether the database is updateable.")]
    public bool? IsUpdateable { get; set; }

    [ReadOnly]
    [Description("Whether the database is a database snapshot.")]
    public bool? IsDatabaseSnapshot { get; set; }

    [ReadOnly]
    [Description("Whether the database is mirroring enabled.")]
    public bool? IsMirroringEnabled { get; set; }

    [ReadOnly]
    [Description("The availability group name if the database is part of an availability group.")]
    public string? AvailabilityGroupName { get; set; }

    [ReadOnly]
    [Description("Whether the database is case sensitive.")]
    public bool? CaseSensitive { get; set; }

    [ReadOnly]
    [Description("The path to the primary file.")]
    public string? PrimaryFilePathActual { get; set; }

    [ReadOnly]
    [Description("The default file group name.")]
    public string? DefaultFileGroup { get; set; }

    // DSC properties
    [JsonPropertyName("_exist")]
    [Description("Indicates whether the database exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
