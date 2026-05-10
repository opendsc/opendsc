// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// Umbrella service interface for all node operations.
/// Implements all capability sub-interfaces; register via this umbrella in DI.
/// </summary>
public interface INodeService
    : INodeReader,
      INodeConfigurationManager,
      INodeTagManager,
      INodeRegistrationManager,
      INodeLcmManager,
      INodeManager
{
}
