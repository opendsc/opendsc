// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

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
    /// SHA256 hash of the node's API key.
    /// </summary>
    public string ApiKeyHash { get; set; } = string.Empty;

    /// <summary>
    /// Last time the node checked in with the server.
    /// </summary>
    public DateTimeOffset? LastCheckIn { get; set; }

    /// <summary>
    /// Current compliance status of the node.
    /// </summary>
    public NodeStatus Status { get; set; } = NodeStatus.Unknown;

    /// <summary>
    /// When the node was registered.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Navigation property for reports.
    /// </summary>
    public ICollection<Report> Reports { get; set; } = [];
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
