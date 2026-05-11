// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

public interface IGroupManager
{
    Task<GroupSummary> CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);

    Task<GroupSummary> UpdateGroupAsync(Guid groupId, UpdateGroupRequest request, CancellationToken cancellationToken = default);

    Task DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    Task AddMemberAsync(Guid groupId, AddGroupMemberRequest request, CancellationToken cancellationToken = default);

    Task RemoveMemberAsync(Guid groupId, RemoveGroupMemberRequest request, CancellationToken cancellationToken = default);

    Task SetMembersAsync(Guid groupId, SetGroupMembersRequest request, CancellationToken cancellationToken = default);

    Task AssignRoleAsync(Guid groupId, AssignGroupRoleRequest request, CancellationToken cancellationToken = default);

    Task RemoveRoleAsync(Guid groupId, RemoveGroupRoleRequest request, CancellationToken cancellationToken = default);

    Task SetRolesAsync(Guid groupId, SetGroupRolesRequest request, CancellationToken cancellationToken = default);

    Task<ExternalGroupMappingInfo?> CreateExternalGroupMappingAsync(CreateExternalGroupMappingRequest request, CancellationToken cancellationToken = default);

    Task DeleteExternalGroupMappingAsync(Guid mappingId, CancellationToken cancellationToken = default);
}
