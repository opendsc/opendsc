// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

public interface IRoleManager
{
    Task<RoleSummary> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    Task<RoleSummary> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task SetGroupsForRoleAsync(Guid roleId, SetRoleGroupsRequest request, CancellationToken cancellationToken = default);
}
