// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Settings;

/// <summary>
/// Read operations for node registration keys.
/// </summary>
public interface IRegistrationKeyReader
{
    Task<IReadOnlyList<RegistrationKeyResponse>> GetKeysAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Write operations for node registration keys.
/// </summary>
public interface IRegistrationKeyManager
{
    Task<RegistrationKeyResponse> CreateKeyAsync(
        CreateRegistrationKeyRequest request,
        CancellationToken cancellationToken = default);

    Task<RegistrationKeyResponse> UpdateKeyAsync(
        Guid id,
        UpdateRegistrationKeyRequest request,
        CancellationToken cancellationToken = default);

    Task RevokeKeyAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RegistrationKeyResponse> RotateKeyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Umbrella service interface for all registration key operations.
/// Implements all capability sub-interfaces; register via this umbrella in DI.
/// </summary>
public interface IRegistrationKeyService : IRegistrationKeyReader, IRegistrationKeyManager
{
}
