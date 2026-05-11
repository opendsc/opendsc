// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

public interface IUserReader
{
    Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken = default);

    Task<UserDetails> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleSummary>?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);

    Task<CurrentUserDetails?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<string?> GetExternalLoginAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetUserRoleCountsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetUserGroupCountsAsync(CancellationToken cancellationToken = default);

    Task<HashSet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}
