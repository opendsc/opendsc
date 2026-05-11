// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Settings;

/// <summary>
/// Read operations for scope types and scope values.
/// </summary>
public interface IScopeReader
{
    Task<IReadOnlyList<ScopeTypeDetails>> GetScopeTypesAsync(CancellationToken cancellationToken = default);

    Task<ScopeTypeDetails> GetScopeTypeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScopeValueDetails>> GetScopeValuesAsync(
        Guid scopeTypeId,
        CancellationToken cancellationToken = default);

    Task<ScopeValueDetails> GetScopeValueAsync(
        Guid scopeTypeId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<int> GetScopeTypeUsageCountAsync(Guid scopeTypeId, CancellationToken cancellationToken = default);

    Task<int> GetScopeValueUsageCountAsync(Guid scopeValueId, CancellationToken cancellationToken = default);

    Task<ScopeSummaryResponse> GetScopeSummaryAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScopeTypeWithValuesDetails>> GetAllScopeTypesWithValuesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScopeNodeInfo>> GetScopeNodesAsync(
        Guid scopeTypeId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScopeParameterInfo>> GetScopeParametersAsync(
        Guid schemaId,
        Guid scopeTypeId,
        string? scopeValue,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetUnrestrictedScopeValuesAsync(
        Guid scopeTypeId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Write operations for scope types and scope values.
/// </summary>
public interface IScopeManager
{
    Task<ScopeTypeDetails> CreateScopeTypeAsync(
        CreateScopeTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<ScopeTypeDetails> UpdateScopeTypeAsync(
        Guid id,
        UpdateScopeTypeRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScopeTypeDetails>> ReorderScopeTypesAsync(
        ReorderScopeTypesRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteScopeTypeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ScopeTypeDetails> EnableScopeTypeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ScopeTypeDetails> DisableScopeTypeAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ScopeValueDetails> CreateScopeValueAsync(
        Guid scopeTypeId,
        CreateScopeValueRequest request,
        CancellationToken cancellationToken = default);

    Task<ScopeValueDetails> UpdateScopeValueAsync(
        Guid scopeTypeId,
        Guid id,
        UpdateScopeValueRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteScopeValueAsync(Guid scopeTypeId, Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Umbrella service interface for all scope operations.
/// Implements all capability sub-interfaces; register via this umbrella in DI.
/// </summary>
public interface IScopeService : IScopeReader, IScopeManager
{
}
