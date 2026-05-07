// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.Lcm;

namespace OpenDsc.Server.Entities;

/// <summary>
/// Records an LCM operational state transition event for a node.
/// </summary>
public sealed class NodeStatusEvent
{
    /// <summary>
    /// Auto-increment primary key.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The node this event belongs to.
    /// </summary>
    public Guid NodeId { get; set; }

    /// <summary>
    /// Navigation property for the node.
    /// </summary>
    public Node Node { get; set; } = null!;

    /// <summary>
    /// The new LCM operational state.
    /// </summary>
    public LcmStatus? LcmStatus { get; set; }

    /// <summary>
    /// When this event was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
}
