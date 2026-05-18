// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

/// <summary>
/// Service for managing roles and their permissions.
/// </summary>
public interface IRoleManager
{
    /// <summary>
    /// Creates a new role with the specified permissions.
    /// </summary>
    /// <param name="request">The role creation request containing role details and permissions.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A summary of the newly created role.</returns>
    Task<RoleSummary> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role, including its name, description, and permissions.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to update.</param>
    /// <param name="request">The role update request containing updated role details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A summary of the updated role.</returns>
    Task<RoleSummary> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets all groups assigned to a role, replacing existing group assignments.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role.</param>
    /// <param name="request">The request containing the complete list of group IDs to assign to the role.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task SetGroupsForRoleAsync(Guid roleId, SetRoleGroupsRequest request, CancellationToken cancellationToken = default);
}
