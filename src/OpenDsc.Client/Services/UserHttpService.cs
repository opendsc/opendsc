// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Client.Http;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Services;

/// <summary>
/// HTTP implementation of user operations.
/// </summary>
public sealed class UserHttpService(HttpClient client)
    : HttpServiceBase(client), IUserReader, IUserManager
{
    private static readonly ClientSerializerContext Ctx = ClientSerializerContext.Default;

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSummary>> GetUsersAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/users", Ctx.UserSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<UserDetails> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => GetAsync($"api/v1/users/{userId}", Ctx.UserDetails, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleSummary>?> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default)
        => await GetOrNullAsync($"api/v1/users/{userId}/roles", Ctx.RoleSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<AuthenticationResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
        => AuthenticateInternalAsync(username, password, cancellationToken);

    /// <inheritdoc />
    public Task<CurrentUserDetails?> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => GetOrNullAsync("api/v1/auth/me", Ctx.CurrentUserDetails, cancellationToken);

    /// <inheritdoc />
    public Task<string?> GetExternalLoginAsync(Guid userId, CancellationToken cancellationToken = default)
        => GetOrNullAsync($"api/v1/users/{userId}/external-login", Ctx.String, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> GetUserRoleCountsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/users/counts/roles", Ctx.GuidIntDictionary, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> GetUserGroupCountsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/users/counts/groups", Ctx.GuidIntDictionary, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<HashSet<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
        => GetAsync($"api/v1/users/{userId}/effective-permissions", Ctx.StringHashSet, cancellationToken);

    /// <inheritdoc />
    public Task<UserSummary> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        => PostAsync("api/v1/users", request, Ctx.CreateUserRequest, Ctx.UserSummary, cancellationToken);

    /// <inheritdoc />
    public Task<UserSummary> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/users/{userId}", request, Ctx.UpdateUserRequest, Ctx.UserSummary, cancellationToken);

    /// <inheritdoc />
    public Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/v1/users/{userId}", cancellationToken);

    /// <inheritdoc />
    public Task ResetPasswordAsync(Guid userId, ResetPasswordRequest request, CancellationToken cancellationToken = default)
        => PostAsync($"api/v1/users/{userId}/reset-password", request, Ctx.ResetPasswordRequest, cancellationToken);

    /// <inheritdoc />
    public Task UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => PostAsync($"api/v1/users/{userId}/unlock", cancellationToken);

    /// <inheritdoc />
    public Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
        => PostAsync("api/v1/auth/change-password", request, Ctx.ChangePasswordRequest, cancellationToken);

    /// <inheritdoc />
    public Task SetUserRolesAsync(Guid userId, SetUserRolesRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/users/{userId}/roles", request, Ctx.SetUserRolesRequest, cancellationToken);

    /// <inheritdoc />
    public Task AssignRoleAsync(Guid userId, AssignRoleRequest request, CancellationToken cancellationToken = default)
        => PostAsync($"api/v1/users/{userId}/roles", request, Ctx.AssignRoleRequest, cancellationToken);

    /// <inheritdoc />
    public Task RemoveRoleAsync(Guid userId, RemoveRoleRequest request, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/v1/users/{userId}/roles/{request.RoleId}", cancellationToken);

    private async Task<AuthenticationResult> AuthenticateInternalAsync(string username, string password, CancellationToken cancellationToken)
    {
        var login = await PostAsync(
            "api/v1/auth/login",
            new LoginRequest { Username = username, Password = password },
            Ctx.LoginRequest,
            Ctx.LoginResult,
            cancellationToken).ConfigureAwait(false);

        return new AuthenticationResult
        {
            IsAuthenticated = true,
            IsLockedOut = false,
            User = new UserSummary
            {
                Id = login.UserId,
                Username = login.Username,
                Email = login.Email,
                RequirePasswordChange = login.RequirePasswordChange,
                IsActive = true
            }
        };
    }
}
