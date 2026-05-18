// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

/// <summary>
/// Service for managing groups, memberships, and group role assignments.
/// </summary>
public interface IGroupManager
{
    /// <summary>
    /// Creates a new group.
    /// </summary>
    /// <param name="request">The group creation request containing group details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A summary of the newly created group.</returns>
    Task<GroupSummary> CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group to update.</param>
    /// <param name="request">The group update request containing updated group details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A summary of the updated group.</returns>
    Task<GroupSummary> UpdateGroupAsync(Guid groupId, UpdateGroupRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a user as a member of a group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <param name="request">The request containing the user ID to add as a member.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task AddMemberAsync(Guid groupId, AddGroupMemberRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a user from group membership.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <param name="request">The request containing the user ID to remove from the group.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task RemoveMemberAsync(Guid groupId, RemoveGroupMemberRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets all members of a group, replacing existing membership.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <param name="request">The request containing the complete list of user IDs for group membership.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task SetMembersAsync(Guid groupId, SetGroupMembersRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a single role to a group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <param name="request">The request containing the role ID to assign to the group.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task AssignRoleAsync(Guid groupId, AssignGroupRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a single role from a group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <param name="request">The request containing the role ID to remove from the group.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task RemoveRoleAsync(Guid groupId, RemoveGroupRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets all roles for a group, replacing existing role assignments.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <param name="request">The request containing the complete list of role IDs.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task SetRolesAsync(Guid groupId, SetGroupRolesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a mapping between an external identity provider group and a local group.
    /// </summary>
    /// <param name="request">The request containing external group mapping details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about the created external group mapping, or null if creation failed.</returns>
    Task<ExternalGroupMappingInfo?> CreateExternalGroupMappingAsync(CreateExternalGroupMappingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an external identity provider group mapping.
    /// </summary>
    /// <param name="mappingId">The unique identifier of the external group mapping to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteExternalGroupMappingAsync(Guid mappingId, CancellationToken cancellationToken = default);
}
