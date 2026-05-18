// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.Lcm;

namespace OpenDsc.Contracts.Settings;

/// <summary>
/// Read operations for server-wide settings.
/// </summary>
public interface ISettingsReader
{
    Task<ServerSettingsSummary> GetServerSettingsAsync(CancellationToken cancellationToken = default);

    Task<ServerLcmDefaultsSummary> GetServerLcmDefaultsAsync(CancellationToken cancellationToken = default);

    Task<PublicSettingsResponse> GetPublicSettingsAsync(CancellationToken cancellationToken = default);

    Task<ValidationSettingsSummary> GetValidationSettingsAsync(CancellationToken cancellationToken = default);

    Task<RetentionSettingsSummary> GetRetentionSettingsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetentionRunSummary>> GetRetentionHistoryAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Write operations for server-wide settings.
/// </summary>
public interface ISettingsManager
{
    Task<ServerSettingsSummary> UpdateServerSettingsAsync(
        UpdateServerSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<ServerLcmDefaultsSummary> UpdateServerLcmDefaultsAsync(
        UpdateServerLcmDefaultsRequest request,
        CancellationToken cancellationToken = default);

    Task<ValidationSettingsSummary> UpdateValidationSettingsAsync(
        UpdateValidationSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<RetentionSettingsSummary> UpdateRetentionSettingsAsync(
        UpdateRetentionSettingsRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Umbrella service interface for all settings operations.
/// Implements all capability sub-interfaces; register via this umbrella in DI.
/// </summary>
public interface ISettingsService : ISettingsReader, ISettingsManager
{
}
