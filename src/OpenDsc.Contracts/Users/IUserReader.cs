// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

/// <summary>
/// Service for reading/retrieving user information.
/// </summary>
public interface IUserReader
{
    /// <summary>
    /// Gets a list of all user accounts.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of user summaries.</returns>
    Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information for a specific user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information for the user, including roles and groups.</returns>
    Task<UserDetails> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of roles assigned to a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of roles assigned to the user, or null if the user is not found.</returns>
    Task<IReadOnlyList<RoleSummary>?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user with the provided username and password.
    /// </summary>
    /// <param name="username">The username to authenticate.</param>
    /// <param name="password">The password for authentication.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The result of the authentication attempt, including user information if successful.</returns>
    Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about the currently authenticated user.
    /// </summary>
    /// <param name="userId">The unique identifier of the authenticated user.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Details about the current user, or null if the user is not found.</returns>
    Task<CurrentUserDetails?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the external login provider information for a user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The external login provider name (e.g., "oidc", "saml"), or null if not available.</returns>
    Task<string?> GetExternalLoginAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of roles assigned to each user.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A dictionary mapping user IDs to the count of roles assigned to each user.</returns>
    Task<IReadOnlyDictionary<Guid, int>> GetUserRoleCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of groups each user belongs to.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A dictionary mapping user IDs to the count of groups each user belongs to.</returns>
    Task<IReadOnlyDictionary<Guid, int>> GetUserGroupCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the set of effective permissions for a user (computed from all roles and groups).
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A set of permission identifiers that the user effectively has.</returns>
    Task<HashSet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
