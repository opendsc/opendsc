// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Client.Http;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Services;

/// <summary>
/// HTTP implementation of group operations.
/// </summary>
public sealed class GroupHttpService(HttpClient client)
    : HttpServiceBase(client), IGroupReader, IGroupManager
{
    private static readonly ClientSerializerContext Ctx = ClientSerializerContext.Default;

    /// <inheritdoc />
    public async Task<IReadOnlyList<GroupSummary>> GetGroupsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/groups", Ctx.GroupSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<GroupDetails> GetGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        => GetAsync($"api/v1/groups/{groupId}", Ctx.GroupDetails, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserSummary>?> GetGroupMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await GetOrNullAsync($"api/v1/groups/{groupId}/members", Ctx.UserSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleSummary>?> GetGroupRolesAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await GetOrNullAsync($"api/v1/groups/{groupId}/roles", Ctx.RoleSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExternalGroupMappingInfo>> GetExternalGroupMappingsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/groups/external-mappings", Ctx.ExternalGroupMappingInfoList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> GetGroupMemberCountsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/groups/counts/members", Ctx.GuidIntDictionary, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<Guid, int>> GetGroupRoleCountsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/groups/counts/roles", Ctx.GuidIntDictionary, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<GroupSummary> CreateGroupAsync(CreateGroupRequest request, CancellationToken cancellationToken = default)
        => PostAsync("api/v1/groups", request, Ctx.CreateGroupRequest, Ctx.GroupSummary, cancellationToken);

    /// <inheritdoc />
    public Task<GroupSummary> UpdateGroupAsync(Guid groupId, UpdateGroupRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/groups/{groupId}", request, Ctx.UpdateGroupRequest, Ctx.GroupSummary, cancellationToken);

    /// <inheritdoc />
    public Task DeleteGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/v1/groups/{groupId}", cancellationToken);

    /// <inheritdoc />
    public Task AddMemberAsync(Guid groupId, AddGroupMemberRequest request, CancellationToken cancellationToken = default)
        => PostAsync($"api/v1/groups/{groupId}/members", request, Ctx.AddGroupMemberRequest, cancellationToken);

    /// <inheritdoc />
    public Task RemoveMemberAsync(Guid groupId, RemoveGroupMemberRequest request, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/v1/groups/{groupId}/members/{request.UserId}", cancellationToken);

    /// <inheritdoc />
    public Task SetMembersAsync(Guid groupId, SetGroupMembersRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/groups/{groupId}/members", request, Ctx.SetGroupMembersRequest, cancellationToken);

    /// <inheritdoc />
    public Task AssignRoleAsync(Guid groupId, AssignGroupRoleRequest request, CancellationToken cancellationToken = default)
        => PostAsync($"api/v1/groups/{groupId}/roles", request, Ctx.AssignGroupRoleRequest, cancellationToken);

    /// <inheritdoc />
    public Task RemoveRoleAsync(Guid groupId, RemoveGroupRoleRequest request, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/v1/groups/{groupId}/roles/{request.RoleId}", cancellationToken);

    /// <inheritdoc />
    public Task SetRolesAsync(Guid groupId, SetGroupRolesRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/groups/{groupId}/roles", request, Ctx.SetGroupRolesRequest, cancellationToken);

    /// <inheritdoc />
    public async Task<ExternalGroupMappingInfo?> CreateExternalGroupMappingAsync(CreateExternalGroupMappingRequest request, CancellationToken cancellationToken = default)
        => await PostAsync("api/v1/groups/external-mappings", request, Ctx.CreateExternalGroupMappingRequest, Ctx.ExternalGroupMappingInfo, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task DeleteExternalGroupMappingAsync(Guid mappingId, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/v1/groups/external-mappings/{mappingId}", cancellationToken);
}
