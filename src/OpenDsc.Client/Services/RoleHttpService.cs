// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Client.Http;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Services;

/// <summary>
/// HTTP implementation of role operations.
/// </summary>
public sealed class RoleHttpService(HttpClient client)
    : HttpServiceBase(client), IRoleReader, IRoleManager
{
    private static readonly ClientSerializerContext Ctx = ClientSerializerContext.Default;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleSummary>> GetRolesAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/roles", Ctx.RoleSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<RoleDetails> GetRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
        => GetAsync($"api/v1/roles/{roleId}", Ctx.RoleDetails, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<GroupSummary>?> GetGroupsForRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
        => await GetOrNullAsync($"api/v1/roles/{roleId}/groups", Ctx.GroupSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> GetRoleUserCountsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/roles/counts/users", Ctx.GuidIntDictionary, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> GetRoleGroupCountsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/roles/counts/groups", Ctx.GuidIntDictionary, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<RoleSummary> CreateRoleAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
        => PostAsync("api/v1/roles", request, Ctx.CreateRoleRequest, Ctx.RoleSummary, cancellationToken);

    /// <inheritdoc />
    public Task<RoleSummary> UpdateRoleAsync(Guid roleId, UpdateRoleRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/roles/{roleId}", request, Ctx.UpdateRoleRequest, Ctx.RoleSummary, cancellationToken);

    /// <inheritdoc />
    public Task DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/v1/roles/{roleId}", cancellationToken);

    /// <inheritdoc />
    public Task SetGroupsForRoleAsync(Guid roleId, SetRoleGroupsRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/roles/{roleId}/groups", request, Ctx.SetRoleGroupsRequest, cancellationToken);
}
