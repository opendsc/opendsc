// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// Configuration assignment and delivery operations for nodes.
/// </summary>
public interface INodeConfigurationManager
{
    Task AssignConfigurationAsync(
        Guid nodeId,
        AssignConfigurationRequest request,
        CancellationToken cancellationToken = default);

    Task RemoveConfigurationAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<NodeConfigurationManifest?> GetNodeConfigurationManifestAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<NodeConfigurationBundle?> GetNodeConfigurationBundleAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<Lcm.ConfigurationChecksumResponse?> GetConfigurationChecksumAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<bool> CheckConfigurationChangedAsync(
        Guid nodeId,
        string etag,
        CancellationToken cancellationToken = default);
}
