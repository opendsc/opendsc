// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// Node lifecycle management operations.
/// </summary>
public interface INodeManager
{
    Task DeleteNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);
}
