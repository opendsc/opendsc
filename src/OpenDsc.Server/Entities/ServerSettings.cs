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
}
