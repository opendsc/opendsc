// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Permission management operations for configurations.
/// </summary>
public interface IConfigurationPermissions
{
    /// <summary>
    /// Gets the list of permissions assigned to a configuration.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of permission entries for the configuration, or null if not found.</returns>
    Task<IReadOnlyList<PermissionEntry>?> GetPermissionsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Grants a permission to a principal (user or group) for a configuration.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="request">The permission grant request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task GrantPermissionAsync(string name, GrantPermissionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a permission from a principal (user or group) for a configuration.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="request">The permission revocation request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task RevokePermissionAsync(string name, RevokePermissionRequest request, CancellationToken cancellationToken = default);
}
