// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// Configuration assignment and delivery operations for nodes.
/// </summary>
public interface INodeConfigurationManager
{
    /// <summary>
    /// Assigns a configuration to the specified node.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="request">The assignment request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task AssignConfigurationAsync(
        Guid nodeId,
        AssignConfigurationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the current configuration assignment from the specified node.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task RemoveConfigurationAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configuration manifest for the specified node.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The configuration manifest, or null if none is assigned.</returns>
    Task<NodeConfigurationManifest?> GetNodeConfigurationManifestAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the configuration bundle (files + manifest) for the specified node.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The configuration bundle, or null if none is assigned.</returns>
    Task<NodeConfigurationBundle?> GetNodeConfigurationBundleAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the checksum of the current configuration for the specified node.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The checksum response, or null if no configuration is assigned.</returns>
    Task<Lcm.ConfigurationChecksumResponse?> GetConfigurationChecksumAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the current configuration differs from the provided ETag.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="etag">The ETag value from the node's last known configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the configuration has changed; otherwise <see langword="false"/>.</returns>
    Task<bool> CheckConfigurationChangedAsync(
        Guid nodeId,
        string etag,
        CancellationToken cancellationToken = default);
}
