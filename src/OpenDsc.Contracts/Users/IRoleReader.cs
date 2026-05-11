// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

public interface IRoleReader
{
    Task<IReadOnlyList<RoleSummary>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<RoleDetails> GetRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<GroupSummary>?> GetGroupsForRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetRoleUserCountsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetRoleGroupCountsAsync(CancellationToken cancellationToken = default);
}
