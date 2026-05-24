// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Client.Http;
using OpenDsc.Contracts.Parameters;
using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Client.Services;

/// <summary>
/// HTTP implementation of parameter operations.
/// </summary>
public sealed class ParameterHttpService(HttpClient client)
    : HttpServiceBase(client), IParameterReader, IParameterManager, IParameterPermissions
{
    private static readonly ClientSerializerContext Ctx = ClientSerializerContext.Default;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ParameterVersionDetails>> GetVersionsAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/parameters/{scopeTypeId}/{configurationId}/versions";
        if (scopeValue is not null)
        {
            url += $"?scopeValue={Uri.EscapeDataString(scopeValue)}";
        }

        return await GetAsync(url, Ctx.ParameterVersionDetailsList, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<string?> GetContentAsync(Guid parameterId, CancellationToken cancellationToken = default)
        => GetOrNullAsync($"api/v1/parameters/versions/{parameterId}/content", Ctx.String, cancellationToken);

    /// <inheritdoc />
    public Task<ParameterProvenanceDetails?> GetNodeProvenanceAsync(Guid nodeId, Guid configurationId, CancellationToken cancellationToken = default)
        => GetOrNullAsync($"api/v1/nodes/{nodeId}/parameters/provenance?configurationId={configurationId}", Ctx.ParameterProvenanceDetails, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetAvailableMajorVersionsAsync(Guid configurationId, CancellationToken cancellationToken = default)
        => await GetAsync($"api/v1/parameters/configurations/{configurationId}/available-majors", Ctx.IntList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<ParameterResolutionDetails?> GetNodeResolutionAsync(Guid nodeId, Guid? configurationId = null, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/nodes/{nodeId}/parameters/resolution";
        if (configurationId is not null)
        {
            url += $"?configurationId={configurationId}";
        }

        return GetOrNullAsync(url, Ctx.ParameterResolutionDetails, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MajorVersionSummary>> GetMajorVersionSummariesAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/parameters/{scopeTypeId}/{configurationId}/majors";
        if (scopeValue is not null)
        {
            url += $"?scopeValue={Uri.EscapeDataString(scopeValue)}";
        }

        return await GetAsync(url, Ctx.MajorVersionSummaryList, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<ParameterVersionDetails?> GetActiveParameterForMajorAsync(
        Guid scopeTypeId,
        Guid configurationId,
        int majorVersion,
        string? scopeValue = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/parameters/{scopeTypeId}/{configurationId}/majors/{majorVersion}";
        if (scopeValue is not null)
        {
            url += $"?scopeValue={Uri.EscapeDataString(scopeValue)}";
        }

        return GetOrNullAsync(url, Ctx.ParameterVersionDetails, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ParameterVersionDetails> CreateAsync(Guid scopeTypeId, Guid configurationId, CreateParameterRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/parameters/{scopeTypeId}/{configurationId}", request, Ctx.CreateParameterRequest, Ctx.ParameterVersionDetails, cancellationToken);

    /// <inheritdoc />
    public Task UpdateAsync(Guid parameterId, UpdateParameterRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/parameters/versions/{parameterId}", request, Ctx.UpdateParameterRequest, cancellationToken);

    /// <inheritdoc />
    public Task PublishAsync(Guid scopeTypeId, Guid configurationId, string? scopeValue, string version, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/parameters/{scopeTypeId}/{configurationId}/versions/{Uri.EscapeDataString(version)}/publish";
        if (scopeValue is not null)
        {
            url += $"?scopeValue={Uri.EscapeDataString(scopeValue)}";
        }

        return PutAsync(url, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(Guid scopeTypeId, Guid configurationId, string? scopeValue, string version, CancellationToken cancellationToken = default)
    {
        var url = $"api/v1/parameters/{scopeTypeId}/{configurationId}/versions/{Uri.EscapeDataString(version)}";
        if (scopeValue is not null)
        {
            url += $"?scopeValue={Uri.EscapeDataString(scopeValue)}";
        }

        return base.DeleteAsync(url, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionEntry>?> GetPermissionsAsync(Guid configurationId, CancellationToken cancellationToken = default)
        => await GetOrNullAsync($"api/v1/parameters/configurations/{configurationId}/permissions", Ctx.PermissionEntryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task GrantPermissionAsync(Guid configurationId, GrantPermissionRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/parameters/configurations/{configurationId}/permissions", request, Ctx.GrantPermissionRequest, cancellationToken);

    /// <inheritdoc />
    public Task RevokePermissionAsync(Guid configurationId, RevokePermissionRequest request, CancellationToken cancellationToken = default)
        => base.DeleteAsync($"api/v1/parameters/configurations/{configurationId}/permissions/{request.PrincipalType}/{request.PrincipalId}", cancellationToken);
}
