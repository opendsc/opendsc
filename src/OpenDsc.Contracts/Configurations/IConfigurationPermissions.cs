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
    Task<List<PermissionEntry>?> GetPermissionsAsync(string name, CancellationToken cancellationToken = default);
    Task GrantPermissionAsync(string name, GrantPermissionRequest request, CancellationToken cancellationToken = default);
    Task RevokePermissionAsync(string name, RevokePermissionRequest request, CancellationToken cancellationToken = default);
}
