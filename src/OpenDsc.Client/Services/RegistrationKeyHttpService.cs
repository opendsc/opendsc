// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Client.Http;
using System.Net.Http.Json;

using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Services;

/// <summary>
/// HTTP implementation of registration key operations.
/// </summary>
public sealed class RegistrationKeyHttpService(HttpClient client)
    : HttpServiceBase(client), IRegistrationKeyService
{
    private static readonly ClientSerializerContext Ctx = ClientSerializerContext.Default;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RegistrationKeyResponse>> GetKeysAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetAsync("api/v1/admin/registration-keys", Ctx.RegistrationKeyResponseList, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<RegistrationKeyResponse> CreateKeyAsync(
        CreateRegistrationKeyRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync("api/v1/admin/registration-keys", request, Ctx.CreateRegistrationKeyRequest, Ctx.RegistrationKeyResponse, cancellationToken);

    /// <inheritdoc />
    public Task<RegistrationKeyResponse> UpdateKeyAsync(
        Guid id,
        UpdateRegistrationKeyRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/admin/registration-keys/{id}", request, Ctx.UpdateRegistrationKeyRequest, Ctx.RegistrationKeyResponse, cancellationToken);

    /// <inheritdoc />
    public Task RevokeKeyAsync(Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync($"api/v1/admin/registration-keys/{id}", cancellationToken);

    /// <inheritdoc />
    public async Task<RegistrationKeyResponse> RotateKeyAsync(CancellationToken cancellationToken = default)
    {
        var response = await Client.PostAsync("api/v1/settings/registration-keys", null, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return (await response.Content.ReadFromJsonAsync(Ctx.RegistrationKeyResponse, cancellationToken).ConfigureAwait(false))!;
    }
}
