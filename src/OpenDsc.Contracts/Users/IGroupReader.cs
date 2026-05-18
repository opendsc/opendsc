// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

/// <summary>
/// Service for reading/retrieving group information and membership.
/// </summary>
public interface IGroupReader
{
    /// <summary>
    /// Gets a list of all groups.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of group summaries.</returns>
    Task<IReadOnlyList<GroupSummary>> GetGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information for a specific group, including members and roles.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information for the group.</returns>
    Task<GroupDetails> GetGroupAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of users who are members of a group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of group members, or null if the group is not found.</returns>
    Task<IReadOnlyList<UserSummary>?> GetGroupMembersAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of roles assigned to a group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of roles assigned to the group, or null if the group is not found.</returns>
    Task<IReadOnlyList<RoleSummary>?> GetGroupRolesAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all external identity provider group mappings.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of external group mapping information.</returns>
    Task<IReadOnlyList<ExternalGroupMappingInfo>> GetExternalGroupMappingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of members in each group.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A dictionary mapping group IDs to the count of members in each group.</returns>
    Task<IReadOnlyDictionary<Guid, int>> GetGroupMemberCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of roles assigned to each group.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A dictionary mapping group IDs to the count of roles assigned to each group.</returns>
    Task<IReadOnlyDictionary<Guid, int>> GetGroupRoleCountsAsync(CancellationToken cancellationToken = default);
}
