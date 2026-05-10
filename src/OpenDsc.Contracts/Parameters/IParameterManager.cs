// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Parameters;

/// <summary>
/// Parameter version lifecycle operations.
/// </summary>
public interface IParameterManager
{
    Task<ParameterVersionDetails> CreateAsync(
        Guid scopeTypeId,
        Guid configurationId,
        CreateParameterRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid parameterId,
        UpdateParameterRequest request,
        CancellationToken cancellationToken = default);

    Task PublishAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        string version,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid scopeTypeId,
        Guid configurationId,
        string? scopeValue,
        string version,
        CancellationToken cancellationToken = default);
}
