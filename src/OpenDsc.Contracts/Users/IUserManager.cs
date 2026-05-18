// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

/// <summary>
/// Service for managing user accounts, passwords, and role assignments.
/// </summary>
public interface IUserManager
{
    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="request">The user creation request containing account details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A summary of the newly created user.</returns>
    Task<UserSummary> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to update.</param>
    /// <param name="request">The user update request containing updated account details.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A summary of the updated user.</returns>
    Task<UserSummary> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets a user's password (admin operation, no current password verification).
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose password to reset.</param>
    /// <param name="request">The password reset request containing the new password.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task ResetPasswordAsync(Guid userId, ResetPasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks a user account that is currently locked due to failed login attempts.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to unlock.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Allows a user to change their own password with current password verification.
    /// </summary>
    /// <param name="userId">The unique identifier of the user changing their password.</param>
    /// <param name="request">The password change request with current and new passwords.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets all roles for a user, replacing existing role assignments.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="request">The request containing the complete list of role IDs.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task SetUserRolesAsync(Guid userId, SetUserRolesRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a single role to a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="request">The request containing the role ID to assign.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task AssignRoleAsync(Guid userId, AssignRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a single role from a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="request">The request containing the role ID to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task RemoveRoleAsync(Guid userId, RemoveRoleRequest request, CancellationToken cancellationToken = default);
}
