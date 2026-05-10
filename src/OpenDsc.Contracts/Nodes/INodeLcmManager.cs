// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// LCM runtime and certificate operations for nodes.
/// </summary>
public interface INodeLcmManager
{
    Task<Lcm.RotateCertificateResponse> RotateCertificateAsync(
        Guid nodeId,
        Lcm.RotateCertificateRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateLcmStatusAsync(
        Guid nodeId,
        Lcm.UpdateLcmStatusRequest request,
        CancellationToken cancellationToken = default);

    Task<Lcm.NodeLcmConfigResponse?> GetNodeLcmConfigAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<Lcm.NodeLcmConfigResponse?> UpdateNodeLcmConfigAsync(
        Guid nodeId,
        UpdateNodeLcmConfigRequest request,
        CancellationToken cancellationToken = default);

    Task ReportNodeLcmConfigAsync(
        Guid nodeId,
        Lcm.ReportNodeLcmConfigRequest request,
        CancellationToken cancellationToken = default);
}
