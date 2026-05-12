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
    Task<ServerSettingsResponse> GetServerSettingsAsync(CancellationToken cancellationToken = default);

    Task<ServerLcmDefaultsResponse> GetServerLcmDefaultsAsync(CancellationToken cancellationToken = default);

    Task<PublicSettingsResponse> GetPublicSettingsAsync(CancellationToken cancellationToken = default);

    Task<ValidationSettingsResponse> GetValidationSettingsAsync(CancellationToken cancellationToken = default);

    Task<RetentionSettingsResponse> GetRetentionSettingsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RetentionRunSummary>> GetRetentionHistoryAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Write operations for server-wide settings.
/// </summary>
public interface ISettingsManager
{
    Task<ServerSettingsResponse> UpdateServerSettingsAsync(
        UpdateServerSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<ServerLcmDefaultsResponse> UpdateServerLcmDefaultsAsync(
        UpdateServerLcmDefaultsRequest request,
        CancellationToken cancellationToken = default);

    Task<ValidationSettingsResponse> UpdateValidationSettingsAsync(
        UpdateValidationSettingsRequest request,
        CancellationToken cancellationToken = default);

    Task<RetentionSettingsResponse> UpdateRetentionSettingsAsync(
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
