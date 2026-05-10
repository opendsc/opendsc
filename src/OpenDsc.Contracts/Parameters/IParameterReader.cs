// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Parameters;

/// <summary>
/// Read-only parameter operations.
/// </summary>
public interface IParameterReader
{
    Task<IReadOnlyList<ParameterVersionDetails>> GetVersionsAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        CancellationToken cancellationToken = default);

    Task<string?> GetContentAsync(
        Guid parameterId,
        CancellationToken cancellationToken = default);

    Task<ParameterProvenanceDetails?> GetNodeProvenanceAsync(
        Guid nodeId,
        Guid configurationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<int>> GetAvailableMajorVersionsAsync(
        Guid configurationId,
        CancellationToken cancellationToken = default);

    Task<ParameterResolutionDetails?> GetNodeResolutionAsync(
        Guid nodeId,
        Guid? configurationId = null,
        CancellationToken cancellationToken = default);
}
