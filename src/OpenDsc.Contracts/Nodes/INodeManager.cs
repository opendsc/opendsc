// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// Node lifecycle management operations.
/// </summary>
public interface INodeManager
{
    /// <summary>
    /// Permanently deletes a node and all its associated data.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);
}
