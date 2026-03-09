// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Lcm.Contracts;

namespace OpenDsc.Server.Entities;

/// <summary>
/// Server-wide settings stored in the database.
/// </summary>
public sealed class ServerSettings
{
    /// <summary>
    /// Unique identifier (singleton row).
    /// </summary>
    public int Id { get; set; } = 1;

    /// <summary>
    /// Default interval for certificate rotation (informational).
    /// </summary>
    public TimeSpan CertificateRotationInterval { get; set; } = TimeSpan.FromDays(60);

    /// <summary>
    /// Multiplier applied to a node's ConfigurationModeInterval to determine when it is considered stale.
    /// A node is stale when: LastCheckIn + (ConfigurationModeInterval × StalenessMultiplier) &lt; UtcNow.
    /// Default is 2.0 (two missed intervals before stale).
    /// </summary>
    public double StalenessMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Server-wide default LCM operating mode applied to all nodes unless overridden at the node level.
    /// </summary>
    public ConfigurationMode? DefaultConfigurationMode { get; set; }

    /// <summary>
    /// Server-wide default LCM configuration mode interval applied to all nodes unless overridden at the node level.
    /// </summary>
    public TimeSpan? DefaultConfigurationModeInterval { get; set; }

    /// <summary>
    /// Server-wide default compliance reporting setting applied to all nodes unless overridden at the node level.
    /// </summary>
    public bool? DefaultReportCompliance { get; set; }

    // ---- Retention Policy ----

    /// <summary>
    /// Whether the scheduled retention background job is enabled.
    /// </summary>
    public bool RetentionEnabled { get; set; } = false;

    /// <summary>
    /// Number of recent non-draft versions to keep per configuration or parameter group.
    /// </summary>
    public int RetentionKeepVersions { get; set; } = 10;

    /// <summary>
    /// Number of days to retain versions. Versions older than this are candidates for deletion.
    /// </summary>
    public int RetentionKeepDays { get; set; } = 90;

    /// <summary>
    /// When true, release (non-prerelease) versions are never automatically deleted.
    /// </summary>
    public bool RetentionKeepReleaseVersions { get; set; } = true;

    /// <summary>
    /// How often the background retention job runs.
    /// </summary>
    public TimeSpan RetentionScheduleInterval { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Maximum number of compliance reports to keep per node.
    /// </summary>
    public int RetentionReportKeepCount { get; set; } = 1000;

    /// <summary>
    /// Compliance reports older than this many days are candidates for deletion.
    /// </summary>
    public int RetentionReportKeepDays { get; set; } = 30;

    /// <summary>
    /// Maximum number of LCM status events to keep per node.
    /// </summary>
    public int RetentionStatusEventKeepCount { get; set; } = 200;

    /// <summary>
    /// LCM status events older than this many days are candidates for deletion.
    /// </summary>
    public int RetentionStatusEventKeepDays { get; set; } = 7;
}
