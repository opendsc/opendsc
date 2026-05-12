// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

public interface IGroupReader
{
    Task<IReadOnlyList<GroupSummary>> GetGroupsAsync(CancellationToken cancellationToken = default);

    Task<GroupDetails> GetGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserSummary>?> GetGroupMembersAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleSummary>?> GetGroupRolesAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExternalGroupMappingInfo>> GetExternalGroupMappingsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetGroupMemberCountsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetGroupRoleCountsAsync(CancellationToken cancellationToken = default);
}
