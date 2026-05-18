// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Parameters;

/// <summary>
/// Parameter version lifecycle operations.
/// </summary>
public interface IParameterManager
{
    /// <summary>
    /// Creates a new parameter version.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="request">The parameter creation request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Details of the newly created parameter version.</returns>
    Task<ParameterVersionDetails> CreateAsync(
        Guid scopeTypeId,
        Guid configurationId,
        CreateParameterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing parameter version.
    /// </summary>
    /// <param name="parameterId">The unique identifier of the parameter version to update.</param>
    /// <param name="request">The parameter update request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task UpdateAsync(
        Guid parameterId,
        UpdateParameterRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a parameter version, making it available for use.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="scopeValue">The scope value for the parameter (or null for configuration-level).</param>
    /// <param name="version">The version identifier to publish.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task PublishAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        string version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a parameter version.
    /// </summary>
    /// <param name="scopeTypeId">The unique identifier of the scope type.</param>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="scopeValue">The scope value for the parameter (or null for configuration-level).</param>
    /// <param name="version">The version identifier to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        string version,
        CancellationToken cancellationToken = default);
}
