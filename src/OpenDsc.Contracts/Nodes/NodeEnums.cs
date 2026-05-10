// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// Compliance status of a node.
/// </summary>
public enum NodeStatus
{
    Unknown,
    Compliant,
    NonCompliant,
    Error
}
