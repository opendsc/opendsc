// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Settings;

/// <summary>
/// Read operations for scope types and scope values.
/// </summary>
public interface IScopeReader
{
    /// <summary>
    /// Gets all scope types defined on the server.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of scope type details.</returns>
    Task<IReadOnlyList<ScopeTypeDetails>> GetScopeTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information for a specific scope type.
    /// </summary>
    /// <param name="id">The unique identifier of the scope type.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information for the scope type.</returns>
    Task<ScopeTypeDetails> GetScopeTypeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all scope values defined for a specific scope type.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of scope values for the scope type.</returns>
    Task<IReadOnlyList<ScopeValueDetails>> GetScopeValuesAsync(
        Guid scopeTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information for a specific scope value.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="id">The unique identifier of the scope value.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information for the scope value.</returns>
    Task<ScopeValueDetails> GetScopeValueAsync(
        Guid scopeTypeId,
        Guid id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of nodes or configurations using a specific scope type.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of items using the scope type.</returns>
    Task<int> GetScopeTypeUsageCountAsync(Guid scopeTypeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of nodes or configurations using a specific scope value.
    /// </summary>
    /// <param name="scopeValueId">The unique identifier of the scope value.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of items using the scope value.</returns>
    Task<int> GetScopeValueUsageCountAsync(Guid scopeValueId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a summary of all scopes and their usage statistics.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Summary information about all scopes.</returns>
    Task<ScopeSummaryResponse> GetScopeSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all scope types with their associated scope values in a single call.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of scope types with their values.</returns>
    Task<IReadOnlyList<ScopeTypeWithValuesDetails>> GetAllScopeTypesWithValuesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the nodes associated with a specific scope type.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of nodes using the scope type.</returns>
    Task<IReadOnlyList<ScopeNodeInfo>> GetScopeNodesAsync(
        Guid scopeTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets parameters defined for a specific scope in a configuration schema.
    /// </summary>
    /// <param name="schemaId">The unique identifier of the parameter schema.</param>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="scopeValue">The specific scope value (or null for configuration-level parameters).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of parameters for the scope.</returns>
    Task<IReadOnlyList<ScopeParameterInfo>> GetScopeParametersAsync(
        Guid schemaId,
        Guid scopeTypeId,
        string? scopeValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all scope values that are unrestricted (user-provided) for a specific scope type.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of unrestricted scope values.</returns>
    Task<IReadOnlyList<string>> GetUnrestrictedScopeValuesAsync(
        Guid scopeTypeId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Write operations for scope types and scope values.
/// </summary>
public interface IScopeManager
{
    /// <summary>
    /// Creates a new scope type.
    /// </summary>
    /// <param name="request">The scope type creation request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information about the created scope type.</returns>
    Task<ScopeTypeDetails> CreateScopeTypeAsync(
        CreateScopeTypeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing scope type.
    /// </summary>
    /// <param name="id">The unique identifier of the scope type to update.</param>
    /// <param name="request">The scope type update request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Updated detailed information about the scope type.</returns>
    Task<ScopeTypeDetails> UpdateScopeTypeAsync(
        Guid id,
        UpdateScopeTypeRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the order of scope types as they appear in the UI and parameter resolution.
    /// </summary>
    /// <param name="request">The request specifying the new scope type order.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of scope types in their new order.</returns>
    Task<IReadOnlyList<ScopeTypeDetails>> ReorderScopeTypesAsync(
        ReorderScopeTypesRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a scope type (and any associated scope values).
    /// </summary>
    /// <param name="id">The unique identifier of the scope type to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteScopeTypeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables a previously disabled scope type.
    /// </summary>
    /// <param name="id">The unique identifier of the scope type to enable.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information about the enabled scope type.</returns>
    Task<ScopeTypeDetails> EnableScopeTypeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables a scope type (nodes and configurations will no longer use it).
    /// </summary>
    /// <param name="id">The unique identifier of the scope type to disable.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information about the disabled scope type.</returns>
    Task<ScopeTypeDetails> DisableScopeTypeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new scope value for a scope type.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="request">The scope value creation request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information about the created scope value.</returns>
    Task<ScopeValueDetails> CreateScopeValueAsync(
        Guid scopeTypeId,
        CreateScopeValueRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing scope value.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="id">The unique identifier of the scope value to update.</param>
    /// <param name="request">The scope value update request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Updated detailed information about the scope value.</returns>
    Task<ScopeValueDetails> UpdateScopeValueAsync(
        Guid scopeTypeId,
        Guid id,
        UpdateScopeValueRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a scope value.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="id">The unique identifier of the scope value to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteScopeValueAsync(Guid scopeTypeId, Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Umbrella service interface for all scope operations.
/// Implements all capability sub-interfaces; register via this umbrella in DI.
/// </summary>
public interface IScopeService : IScopeReader, IScopeManager
{
}
