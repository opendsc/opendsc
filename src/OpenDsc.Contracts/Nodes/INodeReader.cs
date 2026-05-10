// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// Read operations for nodes and node-related views.
/// </summary>
public interface INodeReader
{
    Task<IReadOnlyList<NodeSummary>> GetNodesAsync(
        NodeFilterRequest? filter = null,
        CancellationToken cancellationToken = default);

    Task<NodeDetails?> GetNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<NodeAssignmentSummary?> GetNodeAssignmentAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConfigurationOption>> GetAvailableConfigurationsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConfigurationOption>> GetAvailableCompositeConfigurationsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConfigurationAssignmentOption>> GetAssignableConfigurationsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConfigurationAssignmentOption>> GetAssignableCompositeConfigurationsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reports.ReportSummary>> GetNodeReportsAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NodeStatusEventSummary>> GetNodeStatusEventsAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NodeScopeValueSummary>> GetNodeScopeValuesAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NodeTagSummary>> GetNodeTagsAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScopeTypeSummary>> GetScopeTypesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScopeValueSummary>> GetScopeValuesAsync(
        CancellationToken cancellationToken = default);

    Task SetNodeScopeValueAsync(
        Guid nodeId,
        SetNodeScopeValueRequest request,
        CancellationToken cancellationToken = default);
}