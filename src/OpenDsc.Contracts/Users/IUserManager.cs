// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

public interface IUserManager
{
    Task<UserSummary> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    Task<UserSummary> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);

    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(Guid userId, ResetPasswordRequest request, CancellationToken cancellationToken = default);

    Task UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);

    Task SetUserRolesAsync(Guid userId, SetUserRolesRequest request, CancellationToken cancellationToken = default);

    Task AssignRoleAsync(Guid userId, AssignRoleRequest request, CancellationToken cancellationToken = default);

    Task RemoveRoleAsync(Guid userId, RemoveRoleRequest request, CancellationToken cancellationToken = default);
}
