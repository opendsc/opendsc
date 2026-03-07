// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Lcm.Contracts;

namespace OpenDsc.Server.Entities;

/// <summary>
/// Represents a registered node in the pull server.
/// </summary>
public sealed class Node
{
    /// <summary>
    /// Unique identifier for the node.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Fully qualified domain name of the node.
    /// </summary>
    public string Fqdn { get; set; } = string.Empty;

    /// <summary>
    /// Name of the configuration assigned to this node.
    /// </summary>
    public string? ConfigurationName { get; set; }

    /// <summary>
    /// Whether the node pulls its configuration from the server or manages it locally.
    /// </summary>
    public ConfigurationSource ConfigurationSource { get; set; } = ConfigurationSource.Pull;

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
    /// SHA256 thumbprint of the node's client certificate.
    /// </summary>
    public string CertificateThumbprint { get; set; } = string.Empty;

    /// <summary>
    /// Subject DN of the node's client certificate.
    /// </summary>
    public string CertificateSubject { get; set; } = string.Empty;

    /// <summary>
    /// Expiration date of the node's client certificate.
    /// </summary>
    public DateTimeOffset CertificateNotAfter { get; set; }

    /// <summary>
    /// Last time the node checked in with the server.
    /// </summary>
    public DateTimeOffset? LastCheckIn { get; set; }

    /// <summary>
    /// Current compliance status of the node.
    /// </summary>
    public NodeStatus Status { get; set; } = NodeStatus.Unknown;

    /// <summary>
    /// Current operational state of the LCM agent running on this node.
    /// </summary>
    public LcmStatus LcmStatus { get; set; } = LcmStatus.Unknown;

    /// <summary>
    /// When the node was registered.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Navigation property for reports.
    /// </summary>
    public ICollection<Report> Reports { get; set; } = [];

    /// <summary>
    /// Navigation property for status events.
    /// </summary>
    public ICollection<NodeStatusEvent> StatusEvents { get; set; } = [];
}

/// <summary>
/// Compliance status of a node.
/// </summary>
public enum NodeStatus
{
    /// <summary>
    /// Status is unknown (node has not reported).
    /// </summary>
    Unknown,

    /// <summary>
    /// Node is in desired state.
    /// </summary>
    Compliant,

    /// <summary>
    /// Node is not in desired state.
    /// </summary>
    NonCompliant,

    /// <summary>
    /// Last operation had errors.
    /// </summary>
    Error
}
