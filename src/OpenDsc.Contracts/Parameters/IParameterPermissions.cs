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
    Task<List<PermissionEntry>?> GetPermissionsAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default);

    Task GrantPermissionAsync(
        Guid configurationId,
        GrantPermissionRequest request,
        CancellationToken cancellationToken = default);

    Task RevokePermissionAsync(
        Guid configurationId,
        RevokePermissionRequest request,
        CancellationToken cancellationToken = default);
}
