// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// LCM runtime and certificate operations for nodes.
/// </summary>
public interface INodeLcmManager
{
    /// <summary>
    /// Rotates the certificate for the specified node.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="request">The rotation request containing the new certificate details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The rotation response with any updated credentials.</returns>
    Task<Lcm.RotateCertificateResponse> RotateCertificateAsync(
        Guid nodeId,
        Lcm.RotateCertificateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the LCM operational status reported by the specified node.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="request">The status update request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task UpdateLcmStatusAsync(
        Guid nodeId,
        Lcm.UpdateLcmStatusRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current LCM configuration for the specified node.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The LCM configuration response, or null if not set.</returns>
    Task<Lcm.NodeLcmConfigResponse?> GetNodeLcmConfigAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the LCM configuration for the specified node.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="request">The LCM configuration update request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated LCM configuration response, or null if the node was not found.</returns>
    Task<Lcm.NodeLcmConfigResponse?> UpdateNodeLcmConfigAsync(
        Guid nodeId,
        UpdateNodeLcmConfigRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records the LCM configuration as reported by the node itself.
    /// </summary>
    /// <param name="nodeId">The node's unique identifier.</param>
    /// <param name="request">The reported LCM configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task ReportNodeLcmConfigAsync(
        Guid nodeId,
        Lcm.ReportNodeLcmConfigRequest request,
        CancellationToken cancellationToken = default);
}
