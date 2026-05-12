// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// Tag and scope value management operations for nodes.
/// </summary>
public interface INodeTagManager
{
    Task<NodeTagSummary> AddNodeTagAsync(
        Guid nodeId,
        AddNodeTagRequest request,
        CancellationToken cancellationToken = default);

    Task RemoveNodeTagAsync(
        Guid nodeId,
        RemoveNodeTagRequest request,
        CancellationToken cancellationToken = default);

    Task SetNodeScopeValueAsync(
        Guid nodeId,
        SetNodeScopeValueRequest request,
        CancellationToken cancellationToken = default);
}
