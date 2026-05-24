// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Client.Http;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Services;

/// <summary>
/// HTTP implementation of scope type and scope value operations.
/// </summary>
public sealed class ScopeHttpService(HttpClient client)
    : HttpServiceBase(client), IScopeReader, IScopeManager
{
    private static readonly ClientSerializerContext Ctx = ClientSerializerContext.Default;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScopeTypeDetails>> GetScopeTypesAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/scope-types", Ctx.ScopeTypeDetailsList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<ScopeTypeDetails> GetScopeTypeAsync(Guid id, CancellationToken cancellationToken = default)
        => GetAsync($"api/v1/scope-types/{id}", Ctx.ScopeTypeDetails, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScopeValueDetails>> GetScopeValuesAsync(Guid scopeTypeId, CancellationToken cancellationToken = default)
        => await GetAsync($"api/v1/scope-types/{scopeTypeId}/values", Ctx.ScopeValueDetailsList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<ScopeValueDetails> GetScopeValueAsync(Guid scopeTypeId, Guid id, CancellationToken cancellationToken = default)
        => GetAsync($"api/v1/scope-types/{scopeTypeId}/values/{id}", Ctx.ScopeValueDetails, cancellationToken);

    /// <inheritdoc />
    public Task<int> GetScopeTypeUsageCountAsync(Guid scopeTypeId, CancellationToken cancellationToken = default)
        => GetAsync($"api/v1/scope-types/{scopeTypeId}/usage-count", Ctx.Int32, cancellationToken);

    /// <inheritdoc />
    public Task<int> GetScopeValueUsageCountAsync(Guid scopeValueId, CancellationToken cancellationToken = default)
        => GetAsync($"api/v1/scope-types/values/{scopeValueId}/usage-count", Ctx.Int32, cancellationToken);

    /// <inheritdoc />
    public Task<ScopeSummaryResponse> GetScopeSummaryAsync(CancellationToken cancellationToken = default)
        => GetAsync("api/v1/scope-types/summary", Ctx.ScopeSummaryResponse, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScopeTypeWithValuesDetails>> GetAllScopeTypesWithValuesAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/scope-types/with-values", Ctx.ScopeTypeWithValuesDetailsList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScopeNodeInfo>> GetScopeNodesAsync(Guid scopeTypeId, CancellationToken cancellationToken = default)
        => await GetAsync($"api/v1/scope-types/{scopeTypeId}/nodes", Ctx.ScopeNodeInfoList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScopeParameterInfo>> GetScopeParametersAsync(Guid schemaId, Guid scopeTypeId, string? scopeValue, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/scope-types/parameters/{schemaId}/{scopeTypeId}";
        if (scopeValue is not null)
        {
            url += $"?scopeValue={Uri.EscapeDataString(scopeValue)}";
        }

        return await GetAsync(url, Ctx.ScopeParameterInfoList, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetUnrestrictedScopeValuesAsync(Guid scopeTypeId, CancellationToken cancellationToken = default)
        => await GetAsync($"api/v1/scope-types/{scopeTypeId}/unrestricted-values", Ctx.StringList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<ScopeTypeDetails> CreateScopeTypeAsync(
        CreateScopeTypeRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync("api/v1/scope-types", request, Ctx.CreateScopeTypeRequest, Ctx.ScopeTypeDetails, cancellationToken);

    /// <inheritdoc />
    public Task<ScopeTypeDetails> UpdateScopeTypeAsync(
        Guid id,
        UpdateScopeTypeRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/scope-types/{id}", request, Ctx.UpdateScopeTypeRequest, Ctx.ScopeTypeDetails, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScopeTypeDetails>> ReorderScopeTypesAsync(
        ReorderScopeTypesRequest request,
        CancellationToken cancellationToken = default)
        => await PutAsync("api/v1/scope-types/reorder", request, Ctx.ReorderScopeTypesRequest, Ctx.ScopeTypeDetailsList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task DeleteScopeTypeAsync(Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/v1/scope-types/{id}", cancellationToken);

    /// <inheritdoc />
    public Task<ScopeTypeDetails> EnableScopeTypeAsync(Guid id, CancellationToken cancellationToken = default)
        => PatchAsync($"api/v1/scope-types/{id}/enable", Ctx.ScopeTypeDetails, cancellationToken);

    /// <inheritdoc />
    public Task<ScopeTypeDetails> DisableScopeTypeAsync(Guid id, CancellationToken cancellationToken = default)
        => PatchAsync($"api/v1/scope-types/{id}/disable", Ctx.ScopeTypeDetails, cancellationToken);

    /// <inheritdoc />
    public Task<ScopeValueDetails> CreateScopeValueAsync(
        Guid scopeTypeId,
        CreateScopeValueRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync($"api/v1/scope-types/{scopeTypeId}/values", request, Ctx.CreateScopeValueRequest, Ctx.ScopeValueDetails, cancellationToken);

    /// <inheritdoc />
    public Task<ScopeValueDetails> UpdateScopeValueAsync(
        Guid scopeTypeId,
        Guid id,
        UpdateScopeValueRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/scope-types/{scopeTypeId}/values/{id}", request, Ctx.UpdateScopeValueRequest, Ctx.ScopeValueDetails, cancellationToken);

    /// <inheritdoc />
    public Task DeleteScopeValueAsync(Guid scopeTypeId, Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/v1/scope-types/{scopeTypeId}/values/{id}", cancellationToken);
}
