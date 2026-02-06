// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer.Configuration;

/// <summary>
/// Schema for SQL Server Configuration resource.
/// Manages SQL Server instance-level configuration options (sp_configure settings).
/// </summary>
[Title("SQL Server Configuration")]
[Description("Manages SQL Server instance-level configuration options.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    /// <summary>
    /// SQL Server instance name.
    /// </summary>
    [Description("SQL Server instance name (e.g., '.', 'localhost', 'server\\instance').")]
    public string ServerInstance { get; set; } = string.Empty;

    /// <summary>
    /// Username for SQL Server authentication. If not specified, Windows authentication is used.
    /// </summary>
    [Description("Username for SQL Server authentication. If not specified, Windows authentication is used.")]
    [WriteOnly]
    [Nullable(false)]
    public string? ConnectUsername { get; set; }

    /// <summary>
    /// Password for SQL Server authentication.
    /// </summary>
    [Description("Password for SQL Server authentication.")]
    [WriteOnly]
    [Nullable(false)]
    public string? ConnectPassword { get; set; }

    // Memory Configuration

    /// <summary>
    /// Maximum server memory in MB.
    /// </summary>
    [Description("Maximum server memory in MB. Use 2147483647 for unlimited.")]
    [Nullable(false)]
    public int? MaxServerMemory { get; set; }

    /// <summary>
    /// Minimum server memory in MB.
    /// </summary>
    [Description("Minimum server memory in MB.")]
    [Nullable(false)]
    public int? MinServerMemory { get; set; }

    /// <summary>
    /// Minimum memory per query in KB.
    /// </summary>
    [Description("Minimum memory per query in KB.")]
    [Nullable(false)]
    public int? MinMemoryPerQuery { get; set; }

    // Parallelism Configuration

    /// <summary>
    /// Maximum degree of parallelism.
    /// </summary>
    [Description("Maximum degree of parallelism. 0 = use all available processors.")]
    [Nullable(false)]
    public int? MaxDegreeOfParallelism { get; set; }

    /// <summary>
    /// Cost threshold for parallelism.
    /// </summary>
    [Description("Cost threshold for parallelism. Queries below this cost run serially.")]
    [Nullable(false)]
    public int? CostThresholdForParallelism { get; set; }

    // Network Configuration

    /// <summary>
    /// Network packet size in bytes.
    /// </summary>
    [Description("Network packet size in bytes. Range: 512-32767.")]
    [Nullable(false)]
    public int? NetworkPacketSize { get; set; }

    /// <summary>
    /// Remote login timeout in seconds.
    /// </summary>
    [Description("Remote login timeout in seconds. 0 = infinite.")]
    [Nullable(false)]
    public int? RemoteLoginTimeout { get; set; }

    /// <summary>
    /// Remote query timeout in seconds.
    /// </summary>
    [Description("Remote query timeout in seconds. 0 = no timeout.")]
    [Nullable(false)]
    public int? RemoteQueryTimeout { get; set; }

    // Feature Toggles

    /// <summary>
    /// Enable xp_cmdshell.
    /// </summary>
    [Description("Enable xp_cmdshell extended stored procedure.")]
    [Nullable(false)]
    public bool? XpCmdShellEnabled { get; set; }

    /// <summary>
    /// Enable Database Mail XPs.
    /// </summary>
    [Description("Enable Database Mail extended stored procedures.")]
    [Nullable(false)]
    public bool? DatabaseMailEnabled { get; set; }

    /// <summary>
    /// Enable SQL Server Agent XPs.
    /// </summary>
    [Description("Enable SQL Server Agent extended stored procedures.")]
    [Nullable(false)]
    public bool? AgentXpsEnabled { get; set; }

    /// <summary>
    /// Enable OLE Automation procedures.
    /// </summary>
    [Description("Enable OLE Automation extended stored procedures.")]
    [Nullable(false)]
    public bool? OleAutomationProceduresEnabled { get; set; }

    /// <summary>
    /// Enable ad hoc distributed queries.
    /// </summary>
    [Description("Enable ad hoc distributed queries using OPENROWSET and OPENDATASOURCE.")]
    [Nullable(false)]
    public bool? AdHocDistributedQueriesEnabled { get; set; }

    /// <summary>
    /// Enable CLR integration.
    /// </summary>
    [Description("Enable SQL Server CLR integration.")]
    [Nullable(false)]
    public bool? ClrEnabled { get; set; }

    /// <summary>
    /// Enable remote DAC connections.
    /// </summary>
    [Description("Enable remote Dedicated Administrator Connection (DAC).")]
    [Nullable(false)]
    public bool? RemoteDacConnectionsEnabled { get; set; }

    /// <summary>
    /// Enable contained database authentication.
    /// </summary>
    [Description("Enable contained database authentication across the server instance.")]
    [Nullable(false)]
    public bool? ContainmentEnabled { get; set; }

    // Backup Configuration

    /// <summary>
    /// Default backup compression.
    /// </summary>
    [Description("Enable backup compression by default for all backups.")]
    [Nullable(false)]
    public bool? DefaultBackupCompression { get; set; }

    /// <summary>
    /// Default backup checksum.
    /// </summary>
    [Description("Enable backup checksum by default for all backups.")]
    [Nullable(false)]
    public bool? DefaultBackupChecksum { get; set; }

    // Query Configuration

    /// <summary>
    /// Query governor cost limit.
    /// </summary>
    [Description("Maximum estimated cost for query execution. 0 = no limit.")]
    [Nullable(false)]
    public int? QueryGovernorCostLimit { get; set; }

    /// <summary>
    /// Query wait timeout in seconds.
    /// </summary>
    [Description("Time (seconds) a query waits for resources. -1 = calculated based on estimated query cost.")]
    [Nullable(false)]
    public int? QueryWait { get; set; }

    /// <summary>
    /// Optimize for ad hoc workloads.
    /// </summary>
    [Description("Improve plan cache efficiency for ad hoc queries by caching small compiled plans.")]
    [Nullable(false)]
    public bool? OptimizeAdhocWorkloads { get; set; }

    // Trigger Configuration

    /// <summary>
    /// Nested triggers enabled.
    /// </summary>
    [Description("Allow triggers to fire other triggers (cascade up to 32 levels).")]
    [Nullable(false)]
    public bool? NestedTriggers { get; set; }

    /// <summary>
    /// Server trigger recursion enabled.
    /// </summary>
    [Description("Allow server-level triggers to fire recursively.")]
    [Nullable(false)]
    public bool? ServerTriggerRecursionEnabled { get; set; }

    /// <summary>
    /// Disallow results from triggers.
    /// </summary>
    [Description("Prevent triggers from returning result sets.")]
    [Nullable(false)]
    public bool? DisallowResultsFromTriggers { get; set; }

    // Security Configuration

    /// <summary>
    /// C2 audit mode.
    /// </summary>
    [Description("Enable C2 audit mode for security auditing.")]
    [Nullable(false)]
    public bool? C2AuditMode { get; set; }

    /// <summary>
    /// Common criteria compliance enabled.
    /// </summary>
    [Description("Enable Common Criteria compliance mode.")]
    [Nullable(false)]
    public bool? CommonCriteriaComplianceEnabled { get; set; }

    /// <summary>
    /// Cross database ownership chaining.
    /// </summary>
    [Description("Enable cross-database ownership chaining at the server level.")]
    [Nullable(false)]
    public bool? CrossDbOwnershipChaining { get; set; }

    // Misc Configuration

    /// <summary>
    /// Default trace enabled.
    /// </summary>
    [Description("Enable the default trace for diagnostics.")]
    [Nullable(false)]
    public bool? DefaultTraceEnabled { get; set; }

    /// <summary>
    /// Blocked process threshold in seconds.
    /// </summary>
    [Description("Threshold (seconds) for blocked process reports. 0 = disabled, 5-86400 = enabled.")]
    [Nullable(false)]
    public int? BlockedProcessThreshold { get; set; }

    /// <summary>
    /// Show advanced options.
    /// </summary>
    [Description("Enable display of advanced configuration options.")]
    [Nullable(false)]
    public bool? ShowAdvancedOptions { get; set; }

    /// <summary>
    /// Recovery interval in minutes.
    /// </summary>
    [Description("Maximum time (minutes) per database for recovery. 0 = automatic.")]
    [Nullable(false)]
    public int? RecoveryInterval { get; set; }

    /// <summary>
    /// Fill factor percentage.
    /// </summary>
    [Description("Default fill factor percentage for index pages. 0 or 100 = full pages.")]
    [Nullable(false)]
    public int? FillFactor { get; set; }

    /// <summary>
    /// User connections limit.
    /// </summary>
    [Description("Maximum number of simultaneous user connections. 0 = unlimited.")]
    [Nullable(false)]
    public int? UserConnections { get; set; }

    /// <summary>
    /// Cursor threshold.
    /// </summary>
    [Description("Number of rows for async cursor generation. -1 = all sync, 0 = all async.")]
    [Nullable(false)]
    public int? CursorThreshold { get; set; }

    /// <summary>
    /// FILESTREAM access level.
    /// </summary>
    [Description("FILESTREAM access level: 0 = Disabled, 1 = T-SQL only, 2 = T-SQL and file I/O.")]
    [Nullable(false)]
    public int? FilestreamAccessLevel { get; set; }

    /// <summary>
    /// Max worker threads.
    /// </summary>
    [Description("Maximum worker threads. 0 = automatic based on processors.")]
    [Nullable(false)]
    public int? MaxWorkerThreads { get; set; }

    // Read-only metadata properties

    /// <summary>
    /// Show advanced options current running value.
    /// </summary>
    [Description("Current running value of show advanced options.")]
    [ReadOnly]
    public bool? ShowAdvancedOptionsRunValue { get; set; }
}
