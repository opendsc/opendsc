// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Parameters;

/// <summary>
/// Read-only parameter operations.
/// </summary>
public interface IParameterReader
{
    /// <summary>
    /// Gets all versions of a parameter for a specific configuration and scope.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="scopeValue">The scope value (or null for configuration-level parameters).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of parameter version details.</returns>
    Task<IReadOnlyList<ParameterVersionDetails>> GetVersionsAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the content of a specific parameter version.
    /// </summary>
    /// <param name="parameterId">The unique identifier of the parameter version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The parameter content, or null if the parameter is not found.</returns>
    Task<string?> GetContentAsync(
        Guid parameterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the parameter provenance (source/origin) for a node's assigned configuration.
    /// </summary>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="configurationId">The unique identifier of the configuration assigned to the node.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Provenance details, or null if not available.</returns>
    Task<ParameterProvenanceDetails?> GetNodeProvenanceAsync(
        Guid nodeId,
        Guid configurationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of major version numbers available for a configuration.
    /// </summary>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of major version numbers (e.g., 1, 2, 3).</returns>
    Task<IReadOnlyList<int>> GetAvailableMajorVersionsAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the parameter resolution details for a node (which parameter version would be used).
    /// </summary>
    /// <param name="nodeId">The unique identifier of the node.</param>
    /// <param name="configurationId">Optional configuration ID to check against specific configuration (or null for assigned config).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Parameter resolution details, or null if not available.</returns>
    Task<ParameterResolutionDetails?> GetNodeResolutionAsync(
        Guid nodeId,
        Guid? configurationId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets summary information for all major versions of a parameter.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="scopeValue">The scope value (or null for configuration-level parameters).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of major version summaries.</returns>
    Task<IReadOnlyList<MajorVersionSummary>> GetMajorVersionSummariesAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active (published) parameter version for a specific major version.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="majorVersion">The major version number to retrieve.</param>
    /// <param name="scopeValue">The scope value (or null for configuration-level parameters).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Details of the active parameter version for the major version, or null if not found.</returns>
    Task<ParameterVersionDetails?> GetActiveParameterForMajorAsync(
        Guid scopeTypeId,
        Guid configurationId,
        int majorVersion,
        string? scopeValue = null,
        CancellationToken cancellationToken = default);
}
