// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Settings;

/// <summary>
/// Read operations for node registration keys.
/// </summary>
public interface IRegistrationKeyReader
{
    /// <summary>
    /// Gets all registration keys.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of registration key responses.</returns>
    Task<IReadOnlyList<RegistrationKeyResponse>> GetKeysAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Write operations for node registration keys.
/// </summary>
public interface IRegistrationKeyManager
{
    /// <summary>
    /// Creates a new registration key.
    /// </summary>
    /// <param name="request">The key creation request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The created registration key response.</returns>
    Task<RegistrationKeyResponse> CreateKeyAsync(
        CreateRegistrationKeyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing registration key.
    /// </summary>
    /// <param name="id">The key's unique identifier.</param>
    /// <param name="request">The key update request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated registration key response.</returns>
    Task<RegistrationKeyResponse> UpdateKeyAsync(
        Guid id,
        UpdateRegistrationKeyRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the specified registration key, preventing further use.
    /// </summary>
    /// <param name="id">The key's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task RevokeKeyAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates the registration key by generating a new secret value.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The rotated registration key response with the new secret.</returns>
    Task<RegistrationKeyResponse> RotateKeyAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Umbrella service interface for all registration key operations.
/// Implements all capability sub-interfaces; register via this umbrella in DI.
/// </summary>
public interface IRegistrationKeyService : IRegistrationKeyReader, IRegistrationKeyManager
{
}
