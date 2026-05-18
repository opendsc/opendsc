// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Nodes;

/// <summary>
/// Read operations for nodes and node-related views.
/// </summary>
public interface INodeReader
{
    /// <summary>
    /// Gets a filtered list of nodes.
    /// </summary>
    /// <param name="filter">Optional filter criteria for nodes.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of node summaries matching the filter.</returns>
    Task<IReadOnlyList<NodeSummary>> GetNodesAsync(
        NodeFilterRequest? filter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information for a specific node.
    /// </summary>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed node information, or null if the node is not found.</returns>
    Task<NodeDetails?> GetNodeAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current configuration assignment for a node.
    /// </summary>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The node's configuration assignment summary, or null if not assigned.</returns>
    Task<NodeAssignmentSummary?> GetNodeAssignmentAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of available regular configurations for assignment.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of available configuration options.</returns>
    Task<IReadOnlyList<ConfigurationOption>> GetAvailableConfigurationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of available composite configurations for assignment.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of available composite configuration options.</returns>
    Task<IReadOnlyList<ConfigurationOption>> GetAvailableCompositeConfigurationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of regular configurations that can be assigned to a node.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of assignable configuration options.</returns>
    Task<IReadOnlyList<ConfigurationAssignmentOption>> GetAssignableConfigurationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of composite configurations that can be assigned to a node.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of assignable composite configuration options.</returns>
    Task<IReadOnlyList<ConfigurationAssignmentOption>> GetAssignableCompositeConfigurationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets compliance reports submitted by a specific node.
    /// </summary>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of compliance reports from the node.</returns>
    Task<IReadOnlyList<Reports.ReportSummary>> GetNodeReportsAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets LCM status change events for a node.
    /// </summary>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of node status event summaries.</returns>
    Task<IReadOnlyList<NodeStatusEventSummary>> GetNodeStatusEventsAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the scope values assigned to a node.
    /// </summary>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of scope values assigned to the node.</returns>
    Task<IReadOnlyList<NodeScopeValueSummary>> GetNodeScopeValuesAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the tags assigned to a node.
    /// </summary>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of tags assigned to the node.</returns>
    Task<IReadOnlyList<NodeTagSummary>> GetNodeTagsAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all defined scope types.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of scope type summaries.</returns>
    Task<IReadOnlyList<ScopeTypeSummary>> GetScopeTypesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all defined scope values.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of scope value summaries.</returns>
    Task<IReadOnlyList<ScopeValueSummary>> GetScopeValuesAsync(
        CancellationToken cancellationToken = default);
}
