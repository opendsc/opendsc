// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Client.Http;
using OpenDsc.Contracts.CompositeConfigurations;
using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Client.Services;

/// <summary>
/// HTTP implementation of composite configuration operations.
/// </summary>
public sealed class CompositeConfigurationHttpService(HttpClient client)
    : HttpServiceBase(client),
      ICompositeConfigurationPermissions,
      ICompositeConfigurationReader,
      ICompositeConfigurationManager
{
    private static readonly ClientSerializerContext Ctx = ClientSerializerContext.Default;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionEntry>?> GetPermissionsAsync(
        string name,
        CancellationToken cancellationToken = default)
        => await GetOrNullAsync($"api/v1/composite-configurations/{Uri.EscapeDataString(name)}/permissions", Ctx.PermissionEntryList, cancellationToken).ConfigureAwait(false);
    /// <inheritdoc />
    public Task GrantPermissionAsync(
        string name,
        GrantPermissionRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync(
            $"api/v1/composite-configurations/{Uri.EscapeDataString(name)}/permissions",
            request,
            Ctx.GrantPermissionRequest,
            cancellationToken);

    /// <inheritdoc />
    public Task RevokePermissionAsync(
        string name,
        RevokePermissionRequest request,
        CancellationToken cancellationToken = default)
        => base.DeleteAsync(
            $"api/v1/composite-configurations/{Uri.EscapeDataString(name)}/permissions/{request.PrincipalType}/{request.PrincipalId}",
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompositeConfigurationSummary>> GetCompositeConfigurationsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/composite-configurations", Ctx.CompositeConfigurationSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<CompositeConfigurationDetails?> GetCompositeConfigurationAsync(string name, CancellationToken cancellationToken = default)
        => GetOrNullAsync($"api/v1/composite-configurations/{Uri.EscapeDataString(name)}", Ctx.CompositeConfigurationDetails, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CompositeConfigurationVersionDetails>?> GetVersionsAsync(string name, CancellationToken cancellationToken = default)
        => await GetOrNullAsync($"api/v1/composite-configurations/{Uri.EscapeDataString(name)}/versions", Ctx.CompositeConfigurationVersionDetailsList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<CompositeConfigurationVersionDetails?> GetVersionAsync(string name, string version, CancellationToken cancellationToken = default)
        => GetOrNullAsync($"api/v1/composite-configurations/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}", Ctx.CompositeConfigurationVersionDetails, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ChildConfigurationOption>> GetAvailableChildConfigurationsAsync(IEnumerable<Guid> excludeIds, CancellationToken cancellationToken = default)
        => await GetAsync(
            $"api/v1/composite-configurations/children/available{BuildExcludeIdsQuery(excludeIds)}",
            Ctx.ChildConfigurationOptionList,
            cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<int>> GetAvailableMajorVersionsAsync(Guid configurationId, CancellationToken cancellationToken = default)
        => await GetAsync($"api/v1/composite-configurations/children/{configurationId}/major-versions", Ctx.IntList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<CompositeConfigurationDetails> CreateAsync(CreateCompositeConfigurationRequest request, CancellationToken cancellationToken = default)
        => PostAsync("api/v1/composite-configurations", request, Ctx.CreateCompositeConfigurationRequest, Ctx.CompositeConfigurationDetails, cancellationToken);

    /// <inheritdoc />
    public new Task DeleteAsync(string name, CancellationToken cancellationToken = default)
        => base.DeleteAsync($"api/v1/composite-configurations/{Uri.EscapeDataString(name)}", cancellationToken);

    /// <inheritdoc />
    public Task<CompositeConfigurationVersionDetails> CreateVersionAsync(string name, CreateCompositeConfigurationVersionRequest request, CancellationToken cancellationToken = default)
        => PostAsync($"api/v1/composite-configurations/{Uri.EscapeDataString(name)}/versions", request, Ctx.CreateCompositeConfigurationVersionRequest, Ctx.CompositeConfigurationVersionDetails, cancellationToken);

    /// <inheritdoc />
    public Task CreateVersionFromExistingAsync(string name, CreateCompositeVersionFromExistingRequest request, CancellationToken cancellationToken = default)
        => PostAsync($"api/v1/composite-configurations/{Uri.EscapeDataString(name)}/versions/from-existing", request, Ctx.CreateCompositeVersionFromExistingRequest, cancellationToken);

    /// <inheritdoc />
    public Task PublishVersionAsync(string name, string version, CancellationToken cancellationToken = default)
        => base.PutAsync($"api/v1/composite-configurations/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}/publish", cancellationToken);

    /// <inheritdoc />
    public Task DeleteVersionAsync(string name, string version, CancellationToken cancellationToken = default)
        => base.DeleteAsync($"api/v1/composite-configurations/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}", cancellationToken);

    /// <inheritdoc />
    public Task<CompositeConfigurationItemDetails> AddChildAsync(string name, string version, AddChildConfigurationRequest request, CancellationToken cancellationToken = default)
        => PostAsync($"api/v1/composite-configurations/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}/children", request, Ctx.AddChildConfigurationRequest, Ctx.CompositeConfigurationItemDetails, cancellationToken);

    /// <inheritdoc />
    public Task<CompositeConfigurationItemDetails> UpdateChildAsync(Guid itemId, UpdateChildConfigurationRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/composite-configurations/children/{itemId}", request, Ctx.UpdateChildConfigurationRequest, Ctx.CompositeConfigurationItemDetails, cancellationToken);

    /// <inheritdoc />
    public Task RemoveChildAsync(Guid itemId, CancellationToken cancellationToken = default)
        => base.DeleteAsync($"api/v1/composite-configurations/children/{itemId}", cancellationToken);

    /// <inheritdoc />
    public Task ReorderChildAsync(Guid itemId, int newOrder, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/composite-configurations/children/{itemId}/order/{newOrder}", cancellationToken);

    private static string BuildExcludeIdsQuery(IEnumerable<Guid> excludeIds)
    {
        var ids = excludeIds.ToArray();
        if (ids.Length == 0)
        {
            return string.Empty;
        }

        return "?" + string.Join("&", ids.Select(id => $"excludeIds={id}"));
    }
}
