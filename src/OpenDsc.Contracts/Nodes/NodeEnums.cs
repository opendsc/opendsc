// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// Compliance status of a node.
/// </summary>
public enum NodeStatus
{
    /// <summary>
    /// The node's compliance status is unknown (not yet evaluated).
    /// </summary>
    Unknown,

    /// <summary>
    /// The node is in the desired state (compliant).
    /// </summary>
    Compliant,

    /// <summary>
    /// The node is not in the desired state (non-compliant).
    /// </summary>
    NonCompliant,

    /// <summary>
    /// An error occurred while evaluating the node's compliance.
    /// </summary>
    Error
}
