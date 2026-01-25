// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Schema;

namespace OpenDsc.Server.Entities;

/// <summary>
/// Represents a compliance report from a node.
/// </summary>
public sealed class Report
{
    /// <summary>
    /// Unique identifier for the report.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The node that submitted this report.
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// Navigation property for the node.
    /// </summary>
    public Node Node { get; set; } = null!;

    /// <summary>
    /// When the report was submitted.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// The DSC operation that was performed.
    /// </summary>
    public DscOperation Operation { get; set; }

    /// <summary>
    /// Whether all resources were in desired state.
    /// </summary>
    public bool InDesiredState { get; set; }

    /// <summary>
    /// Whether the operation had errors.
    /// </summary>
    public bool HadErrors { get; set; }

    /// <summary>
    /// Serialized DscResult JSON for full audit details.
    /// </summary>
    public string ResultJson { get; set; } = string.Empty;
}
