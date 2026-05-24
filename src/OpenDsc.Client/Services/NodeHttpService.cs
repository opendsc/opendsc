// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Client.Http;
using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Reports;

namespace OpenDsc.Client.Services;

/// <summary>
/// HTTP implementation of node operations.
/// </summary>
public sealed class NodeHttpService(HttpClient client)
    : HttpServiceBase(client),
      INodeManager,
      INodeReader,
      INodeConfigurationManager,
      INodeTagManager
{
    private static readonly ClientSerializerContext Ctx = ClientSerializerContext.Default;

    /// <inheritdoc />
    public Task DeleteNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/v1/nodes/{nodeId}", cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<NodeSummary>> GetNodesAsync(NodeFilterRequest? filter = null, CancellationToken cancellationToken = default)
    {
        var url = BuildUrlWithQuery(
            "api/v1/nodes",
            ("fqdnContains", filter?.FqdnContains),
            ("configurationContains", filter?.ConfigurationContains),
            ("status", filter?.Status?.ToString()),
            ("lcmStatus", filter?.LcmStatus?.ToString()),
            ("limit", filter?.Limit?.ToString()));

        return await GetAsync(url, Ctx.NodeSummaryList, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<NodeDetails?> GetNodeAsync(Guid nodeId, CancellationToken cancellationToken = default)
        => GetOrNullAsync($"api/v1/nodes/{nodeId}", Ctx.NodeDetails, cancellationToken);

    /// <inheritdoc />
    public Task<NodeAssignmentSummary?> GetNodeAssignmentAsync(Guid nodeId, CancellationToken cancellationToken = default)
        => GetOrNullAsync($"api/v1/nodes/{nodeId}/assignment", Ctx.NodeAssignmentSummary, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationOption>> GetAvailableConfigurationsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/nodes/available-configurations", Ctx.ConfigurationOptionList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationOption>> GetAvailableCompositeConfigurationsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/nodes/available-composite-configurations", Ctx.ConfigurationOptionList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationAssignmentOption>> GetAssignableConfigurationsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/nodes/assignable-configurations", Ctx.ConfigurationAssignmentOptionList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationAssignmentOption>> GetAssignableCompositeConfigurationsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/nodes/assignable-composite-configurations", Ctx.ConfigurationAssignmentOptionList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReportSummary>> GetNodeReportsAsync(Guid nodeId, CancellationToken cancellationToken = default)
        => await GetAsync($"api/v1/nodes/{nodeId}/reports", Ctx.ReportSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<NodeStatusEventSummary>> GetNodeStatusEventsAsync(Guid nodeId, CancellationToken cancellationToken = default)
        => await GetAsync($"api/v1/nodes/{nodeId}/status-history", Ctx.NodeStatusEventSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<NodeScopeValueSummary>> GetNodeScopeValuesAsync(Guid nodeId, CancellationToken cancellationToken = default)
        => await GetAsync($"api/v1/nodes/{nodeId}/scope-values", Ctx.NodeScopeValueSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<NodeTagSummary>> GetNodeTagsAsync(Guid nodeId, CancellationToken cancellationToken = default)
        => await GetAsync($"api/v1/nodes/{nodeId}/tags", Ctx.NodeTagSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScopeTypeSummary>> GetScopeTypesAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/scope-types", Ctx.ScopeTypeSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ScopeValueSummary>> GetScopeValuesAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/scope-values", Ctx.ScopeValueSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task AssignConfigurationAsync(Guid nodeId, AssignConfigurationRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/nodes/{nodeId}/configuration", request, Ctx.AssignConfigurationRequest, cancellationToken);

    /// <inheritdoc />
    public Task RemoveConfigurationAsync(Guid nodeId, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/v1/nodes/{nodeId}/configuration", cancellationToken);

    /// <inheritdoc />
    public Task<NodeConfigurationManifest?> GetNodeConfigurationManifestAsync(Guid nodeId, CancellationToken cancellationToken = default)
        => GetOrNullAsync($"api/v1/nodes/{nodeId}/configuration/manifest", Ctx.NodeConfigurationManifest, cancellationToken);

    /// <inheritdoc />
    public async Task<NodeConfigurationBundle?> GetNodeConfigurationBundleAsync(Guid nodeId, CancellationToken cancellationToken = default)
    {
        var response = await Client.GetAsync($"api/v1/nodes/{nodeId}/configuration/bundle/download", cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);

        var content = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName
            ?? $"{nodeId}.zip";

        return new NodeConfigurationBundle
        {
            Content = content,
            FileName = fileName.Trim('"'),
            ContentType = response.Content.Headers.ContentType?.MediaType ?? "application/zip"
        };
    }

    /// <inheritdoc />
    public Task<ConfigurationChecksumResponse?> GetConfigurationChecksumAsync(Guid nodeId, CancellationToken cancellationToken = default)
        => GetOrNullAsync($"api/v1/nodes/{nodeId}/configuration/checksum", Ctx.ConfigurationChecksumResponse, cancellationToken);

    /// <inheritdoc />
    public Task<bool> CheckConfigurationChangedAsync(Guid nodeId, string etag, CancellationToken cancellationToken = default)
        => CheckConfigurationChangedInternalAsync(nodeId, etag, cancellationToken);

    /// <inheritdoc />
    public Task<NodeTagSummary> AddNodeTagAsync(Guid nodeId, AddNodeTagRequest request, CancellationToken cancellationToken = default)
        => PostAsync($"api/v1/nodes/{nodeId}/tags", request, Ctx.AddNodeTagRequest, Ctx.NodeTagSummary, cancellationToken);

    /// <inheritdoc />
    public Task RemoveNodeTagAsync(Guid nodeId, RemoveNodeTagRequest request, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/v1/nodes/{nodeId}/tags/{request.ScopeValueId}", cancellationToken);

    /// <inheritdoc />
    public Task SetNodeScopeValueAsync(Guid nodeId, SetNodeScopeValueRequest request, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/nodes/{nodeId}/scope-values", request, Ctx.SetNodeScopeValueRequest, cancellationToken);

    private async Task<bool> CheckConfigurationChangedInternalAsync(Guid nodeId, string etag, CancellationToken cancellationToken)
    {
        var checksum = await GetConfigurationChecksumAsync(nodeId, cancellationToken).ConfigureAwait(false);
        return checksum is null || !string.Equals(checksum.Checksum, etag, StringComparison.OrdinalIgnoreCase);
    }
}
