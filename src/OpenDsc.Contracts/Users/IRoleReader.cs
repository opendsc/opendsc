// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

/// <summary>
/// Service for reading/retrieving role information.
/// </summary>
public interface IRoleReader
{
    /// <summary>
    /// Gets a list of all roles.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of role summaries.</returns>
    Task<IReadOnlyList<RoleSummary>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information for a specific role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information for the role, including permissions.</returns>
    Task<RoleDetails> GetRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of groups that are assigned a specific role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of groups assigned the role, or null if the role is not found.</returns>
    Task<IReadOnlyList<GroupSummary>?> GetGroupsForRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of users assigned to each role.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A dictionary mapping role IDs to the count of users assigned to each role.</returns>
    Task<IReadOnlyDictionary<Guid, int>> GetRoleUserCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of groups assigned to each role.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A dictionary mapping role IDs to the count of groups assigned to each role.</returns>
    Task<IReadOnlyDictionary<Guid, int>> GetRoleGroupCountsAsync(CancellationToken cancellationToken = default);
}
