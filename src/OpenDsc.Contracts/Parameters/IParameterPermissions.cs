// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Contracts.Parameters;

/// <summary>
/// Permission management operations for parameters.
/// </summary>
public interface IParameterPermissions
{
    /// <summary>
    /// Gets the list of permissions assigned to a configuration's parameters.
    /// </summary>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of permission entries for the configuration's parameters, or null if not found.</returns>
    Task<IReadOnlyList<PermissionEntry>?> GetPermissionsAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Grants a permission to a principal (user or group) for a configuration's parameters.
    /// </summary>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="request">The permission grant request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task GrantPermissionAsync(
        Guid configurationId,
        GrantPermissionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a permission from a principal (user or group) for a configuration's parameters.
    /// </summary>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="request">The permission revocation request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task RevokePermissionAsync(
        Guid configurationId,
        RevokePermissionRequest request,
        CancellationToken cancellationToken = default);
}
