// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// Node registration and registration-setting operations.
/// </summary>
public interface INodeRegistrationManager
{
    /// <summary>
    /// Registers a new node with the pull server.
    /// </summary>
    /// <param name="request">The node registration request.</param>
    /// <param name="certificateThumbprint">Optional thumbprint of the client certificate presented during registration.</param>
    /// <param name="certificateSubject">Optional subject of the client certificate presented during registration.</param>
    /// <param name="certificateNotAfter">Optional expiry of the client certificate presented during registration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The registration response containing the assigned node identifier.</returns>
    Task<Lcm.RegisterNodeResponse> RegisterNodeAsync(
        Lcm.RegisterNodeRequest request,
        string? certificateThumbprint,
        string? certificateSubject,
        DateTimeOffset? certificateNotAfter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current server-side registration settings.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The registration settings summary.</returns>
    Task<RegistrationSettingsSummary> GetRegistrationSettingsAsync(
        CancellationToken cancellationToken = default);
}
