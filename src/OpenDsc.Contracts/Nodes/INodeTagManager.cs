// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// Tag and scope value management operations for nodes.
/// </summary>
public interface INodeTagManager
{
    /// <summary>
    /// Adds a scope tag to the specified node.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="request">The tag addition request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created node tag summary.</returns>
    Task<NodeTagSummary> AddNodeTagAsync(
        Guid nodeId,
        AddNodeTagRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a scope tag from the specified node.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="request">The tag removal request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task RemoveNodeTagAsync(
        Guid nodeId,
        RemoveNodeTagRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a specific scope value on the specified node.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="request">The scope value assignment request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task SetNodeScopeValueAsync(
        Guid nodeId,
        SetNodeScopeValueRequest request,
        CancellationToken cancellationToken = default);
}
