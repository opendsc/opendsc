// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Client.Http;
using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Retention;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Services;

/// <summary>
/// HTTP implementation of server-wide settings operations.
/// </summary>
public sealed class SettingsHttpService(HttpClient client)
    : HttpServiceBase(client), ISettingsReader, ISettingsManager
{
    private static readonly ClientSerializerContext Ctx = ClientSerializerContext.Default;

    /// <inheritdoc />
    public Task<ServerSettingsSummary> GetServerSettingsAsync(CancellationToken cancellationToken = default)
        => GetAsync("api/v1/settings", Ctx.ServerSettingsSummary, cancellationToken);

    /// <inheritdoc />
    public Task<ServerLcmDefaultsSummary> GetServerLcmDefaultsAsync(CancellationToken cancellationToken = default)
        => GetAsync("api/v1/settings/lcm-defaults", Ctx.ServerLcmDefaultsSummary, cancellationToken);

    /// <inheritdoc />
    public Task<PublicSettingsResponse> GetPublicSettingsAsync(CancellationToken cancellationToken = default)
        => GetAsync("api/v1/settings/public", Ctx.PublicSettingsResponse, cancellationToken);

    /// <inheritdoc />
    public Task<ValidationSettingsSummary> GetValidationSettingsAsync(CancellationToken cancellationToken = default)
        => GetAsync("api/v1/settings/validation", Ctx.ValidationSettingsSummary, cancellationToken);

    /// <inheritdoc />
    public Task<RetentionSettingsSummary> GetRetentionSettingsAsync(CancellationToken cancellationToken = default)
        => GetAsync("api/v1/settings/retention", Ctx.RetentionSettingsSummary, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RetentionRunSummary>> GetRetentionHistoryAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/retention/runs", Ctx.RetentionRunSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<ServerSettingsSummary> UpdateServerSettingsAsync(
        UpdateServerSettingsRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync("api/v1/settings", request, Ctx.UpdateServerSettingsRequest, Ctx.ServerSettingsSummary, cancellationToken);

    /// <inheritdoc />
    public Task<ServerLcmDefaultsSummary> UpdateServerLcmDefaultsAsync(
        UpdateServerLcmDefaultsRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync("api/v1/settings/lcm-defaults", request, Ctx.UpdateServerLcmDefaultsRequest, Ctx.ServerLcmDefaultsSummary, cancellationToken);

    /// <inheritdoc />
    public Task<ValidationSettingsSummary> UpdateValidationSettingsAsync(
        UpdateValidationSettingsRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync("api/v1/settings/validation", request, Ctx.UpdateValidationSettingsRequest, Ctx.ValidationSettingsSummary, cancellationToken);

    /// <inheritdoc />
    public Task<RetentionSettingsSummary> UpdateRetentionSettingsAsync(
        UpdateRetentionSettingsRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync("api/v1/settings/retention", request, Ctx.UpdateRetentionSettingsRequest, Ctx.RetentionSettingsSummary, cancellationToken);
}
