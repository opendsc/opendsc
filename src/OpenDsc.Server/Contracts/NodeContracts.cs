// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using OpenDsc.Lcm.Contracts;

namespace OpenDsc.Server.Contracts;

/// <summary>
/// Summary information about a node.
/// </summary>
public sealed class NodeSummary
{
    /// <summary>
    /// The node's unique identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The node's fully qualified domain name.
    /// </summary>
    public string Fqdn { get; set; } = string.Empty;

    /// <summary>
    /// The name of the assigned configuration.
    /// </summary>
    public string? ConfigurationName { get; set; }

    /// <summary>
    /// The node's compliance status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// When the node last checked in.
    /// </summary>
    public DateTimeOffset? LastCheckIn { get; set; }

    /// <summary>
    /// When the node was registered.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Whether the node pulls its configuration from the server or manages it locally.
    /// </summary>
    public ConfigurationSource ConfigurationSource { get; set; }

    /// <summary>
    /// The LCM operating mode reported by the node.
    /// </summary>
    public ConfigurationMode? ConfigurationMode { get; set; }

    /// <summary>
    /// The LCM configuration mode interval reported by the node.
    /// </summary>
    public TimeSpan? ConfigurationModeInterval { get; set; }

    /// <summary>
    /// Whether the node submits compliance reports to the server.
    /// </summary>
    public bool? ReportCompliance { get; set; }
}

/// <summary>
/// Request to assign a configuration to a node.
/// </summary>
public sealed class AssignConfigurationRequest
{
    /// <summary>
    /// The name of the configuration to assign.
    /// </summary>
    [JsonRequired]
    public string ConfigurationName { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a composite configuration.
    /// </summary>
    public bool IsComposite { get; set; }

    /// <summary>
    /// The major version to track. When set, the node auto-promotes within this major version only.
    /// When null, the node receives the latest version across all major versions.
    /// Example: 1 tracks 1.x.y, 2 tracks 2.x.y
    /// </summary>
    public int? MajorVersion { get; set; }

    /// <summary>
    /// The minimum prerelease channel threshold (free-text, semver-compared).
    /// When null, only stable (non-prerelease) versions are received.
    /// Example: "rc" receives rc.*, beta.*, and stable; "beta" receives beta.*, rc.*, and stable.
    /// </summary>
    public string? PrereleaseChannel { get; set; }
}

