// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Lcm;

namespace OpenDsc.Contracts.Nodes;

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
    /// The node's LCM operational status.
    /// </summary>
    public string LcmStatus { get; set; } = string.Empty;

    /// <summary>
    /// Whether the node is considered stale (no check-in within ConfigurationModeInterval × StalenessMultiplier).
    /// </summary>
    public bool IsStale { get; set; }

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

    /// <summary>
    /// The desired LCM operating mode set by the server administrator.
    /// </summary>
    public ConfigurationMode? DesiredConfigurationMode { get; set; }

    /// <summary>
    /// The desired LCM configuration mode interval set by the server administrator.
    /// </summary>
    public TimeSpan? DesiredConfigurationModeInterval { get; set; }

    /// <summary>
    /// Whether compliance reporting should be enabled, as set by the server administrator.
    /// </summary>
    public bool? DesiredReportCompliance { get; set; }
}

/// <summary>
/// Summary of a single node status event.
/// </summary>
public sealed class NodeStatusEventSummary
{
    /// <summary>
    /// The event identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The node this event belongs to.
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// Node FQDN.
    /// </summary>
    public string NodeFqdn { get; set; } = string.Empty;

    /// <summary>
    /// The new LCM operational state.
    /// </summary>
    public string? LcmStatus { get; set; }

    /// <summary>
    /// When this event was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
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

/// <summary>
/// Request to update the desired LCM configuration for a node.
/// </summary>
public sealed class UpdateNodeLcmConfigRequest
{
    /// <summary>
    /// The desired LCM operating mode, or null to clear the server-managed value.
    /// </summary>
    public ConfigurationMode? ConfigurationMode { get; set; }

    /// <summary>
    /// The desired interval between LCM operations, or null to clear the server-managed value.
    /// </summary>
    public TimeSpan? ConfigurationModeInterval { get; set; }

    /// <summary>
    /// Whether the node should submit compliance reports, or null to clear the server-managed value.
    /// </summary>
    public bool? ReportCompliance { get; set; }
}

/// <summary>
/// Optional filters used when listing nodes.
/// </summary>
public sealed class NodeFilterRequest
{
    /// <summary>
    /// Optional FQDN search text.
    /// </summary>
    public string? FqdnContains { get; set; }

    /// <summary>
    /// Optional assigned configuration search text.
    /// </summary>
    public string? ConfigurationContains { get; set; }

    /// <summary>
    /// Optional compliance status filter.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Optional LCM status filter.
    /// </summary>
    public string? LcmStatus { get; set; }

    /// <summary>
    /// Optional result size cap.
    /// </summary>
    public int? Limit { get; set; }
}

/// <summary>
/// Detailed node information used by node details views.
/// </summary>
public sealed class NodeDetails
{
    /// <summary>
    /// Node summary fields.
    /// </summary>
    public required NodeSummary Summary { get; set; }

    public Guid Id { get; set; }

    public string Fqdn { get; set; } = string.Empty;

    public string? ConfigurationName { get; set; }

    public string Status { get; set; } = string.Empty;

    public string LcmStatus { get; set; } = string.Empty;

    public bool IsStale { get; set; }

    public DateTimeOffset? LastCheckIn { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ConfigurationSource ConfigurationSource { get; set; }

    public ConfigurationMode? ConfigurationMode { get; set; }

    public TimeSpan? ConfigurationModeInterval { get; set; }

    public bool? ReportCompliance { get; set; }

    public ConfigurationMode? DesiredConfigurationMode { get; set; }

    public TimeSpan? DesiredConfigurationModeInterval { get; set; }

    public bool? DesiredReportCompliance { get; set; }

    /// <summary>
    /// Certificate thumbprint currently associated with this node.
    /// </summary>
    public string CertificateThumbprint { get; set; } = string.Empty;

    /// <summary>
    /// Certificate subject currently associated with this node.
    /// </summary>
    public string CertificateSubject { get; set; } = string.Empty;

    /// <summary>
    /// Certificate expiration currently associated with this node.
    /// </summary>
    public DateTimeOffset CertificateNotAfter { get; set; }
}

/// <summary>
/// A selectable configuration option for node assignment UX.
/// </summary>
public sealed class ConfigurationOption
{
    /// <summary>
    /// Configuration identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Configuration display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Configuration option with available major versions for assignment UX.
/// </summary>
public sealed class ConfigurationAssignmentOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IReadOnlyList<int> MajorVersions { get; set; } = [];
}

/// <summary>
/// Node configuration manifest payload.
/// </summary>
public sealed class NodeConfigurationManifest
{
    /// <summary>
    /// Configuration document content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Entry point path.
    /// </summary>
    public string EntryPoint { get; set; } = "main.dsc.yaml";

    /// <summary>
    /// Optional parameters file path.
    /// </summary>
    public string? ParametersFile { get; set; }

    /// <summary>
    /// Optional content checksum.
    /// </summary>
    public string? Checksum { get; set; }
}

/// <summary>
/// Result of generating a node configuration bundle.
/// </summary>
public sealed class NodeConfigurationBundle
{
    public byte[] Content { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/zip";
}

/// <summary>
/// Node tag summary used by UI and API responses.
/// </summary>
public sealed class NodeTagSummary
{
    /// <summary>
    /// Node identifier.
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// Scope type identifier.
    /// </summary>
    public Guid ScopeTypeId { get; set; }

    /// <summary>
    /// Scope value identifier.
    /// </summary>
    public Guid ScopeValueId { get; set; }

    /// <summary>
    /// Scope type name.
    /// </summary>
    public string ScopeTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Scope value display text.
    /// </summary>
    public string ScopeValue { get; set; } = string.Empty;

    /// <summary>
    /// Scope precedence.
    /// </summary>
    public int Precedence { get; set; }

    /// <summary>
    /// Assignment timestamp.
    /// </summary>
    public DateTimeOffset AssignedAt { get; set; }
}

/// <summary>
/// Request to add a scope value tag to a node.
/// </summary>
public sealed class AddNodeTagRequest
{
    /// <summary>
    /// Scope value identifier to assign.
    /// </summary>
    public Guid ScopeValueId { get; set; }
}

/// <summary>
/// Request to remove a scope value tag from a node.
/// </summary>
public sealed class RemoveNodeTagRequest
{
    /// <summary>
    /// Scope value identifier to remove.
    /// </summary>
    public Guid ScopeValueId { get; set; }
}

/// <summary>
/// Summary of a node scope value association.
/// </summary>
public sealed class NodeScopeValueSummary
{
    /// <summary>
    /// Scope type identifier.
    /// </summary>
    public Guid ScopeTypeId { get; set; }

    /// <summary>
    /// Scope type name.
    /// </summary>
    public string ScopeTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Scope value identifier.
    /// </summary>
    public Guid ScopeValueId { get; set; }

    /// <summary>
    /// Scope value text.
    /// </summary>
    public string ScopeValue { get; set; } = string.Empty;

    /// <summary>
    /// Scope precedence.
    /// </summary>
    public int Precedence { get; set; }
}

/// <summary>
/// Summary of the current configuration assignment for a node.
/// </summary>
public sealed class NodeAssignmentSummary
{
    /// <summary>
    /// Node identifier.
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// Assigned configuration name.
    /// </summary>
    public string? ConfigurationName { get; set; }

    /// <summary>
    /// Whether the assignment targets a composite configuration.
    /// </summary>
    public bool IsComposite { get; set; }

    /// <summary>
    /// Tracked major version.
    /// </summary>
    public int? MajorVersion { get; set; }

    /// <summary>
    /// Prerelease channel threshold.
    /// </summary>
    public string? PrereleaseChannel { get; set; }

    /// <summary>
    /// When the node was assigned.
    /// </summary>
    public DateTimeOffset? AssignedAt { get; set; }
}

/// <summary>
/// Summary of a scope type for node tag selection.
/// </summary>
public sealed class ScopeTypeSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Precedence { get; set; }
    public ScopeValueMode ValueMode { get; set; }
}

/// <summary>
/// Summary of a scope value for node tag selection.
/// </summary>
public sealed class ScopeValueSummary
{
    public Guid Id { get; set; }
    public Guid ScopeTypeId { get; set; }
    public string ScopeTypeName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Precedence { get; set; }
}

/// <summary>
/// Request to set a scope value association for a node.
/// </summary>
public sealed class SetNodeScopeValueRequest
{
    /// <summary>
    /// Scope type identifier.
    /// </summary>
    public Guid ScopeTypeId { get; set; }

    /// <summary>
    /// Scope value text.
    /// </summary>
    public string ScopeValue { get; set; } = string.Empty;
}

/// <summary>
/// Registration setting summary used by node list and registration UI.
/// </summary>
public sealed class RegistrationSettingsSummary
{
    /// <summary>
    /// Whether registrations are currently possible.
    /// </summary>
    public bool RegistrationEnabled { get; set; }

    /// <summary>
    /// Number of active registration keys.
    /// </summary>
    public int ActiveKeyCount { get; set; }
}

