// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Retention;

namespace OpenDsc.Contracts.Settings;

/// <summary>
/// Read operations for server-wide settings.
/// </summary>
public interface ISettingsReader
{
    /// <summary>
    /// Gets the server-wide configuration settings.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Server settings summary.</returns>
    Task<ServerSettingsSummary> GetServerSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default LCM settings applied to nodes on first registration.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Server LCM defaults summary.</returns>
    Task<ServerLcmDefaultsSummary> GetServerLcmDefaultsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets public-facing server settings (accessible without authentication).
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Public settings information.</returns>
    Task<PublicSettingsResponse> GetPublicSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the server-wide parameter validation settings.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Validation settings summary.</returns>
    Task<ValidationSettingsSummary> GetValidationSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the server-wide retention policy settings.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Retention settings summary.</returns>
    Task<RetentionSettingsSummary> GetRetentionSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the history of retention policy executions.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of retention run summaries.</returns>
    Task<IReadOnlyList<RetentionRunSummary>> GetRetentionHistoryAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Write operations for server-wide settings.
/// </summary>
public interface ISettingsManager
{
    /// <summary>
    /// Updates the server-wide configuration settings.
    /// </summary>
    /// <param name="request">The settings update request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Updated server settings summary.</returns>
    Task<ServerSettingsSummary> UpdateServerSettingsAsync(
        UpdateServerSettingsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the default LCM settings for nodes on registration.
    /// </summary>
    /// <param name="request">The LCM defaults update request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Updated server LCM defaults summary.</returns>
    Task<ServerLcmDefaultsSummary> UpdateServerLcmDefaultsAsync(
        UpdateServerLcmDefaultsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the parameter validation mode server-wide.
    /// </summary>
    /// <param name="request">The validation settings update request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Updated validation settings summary.</returns>
    Task<ValidationSettingsSummary> UpdateValidationSettingsAsync(
        UpdateValidationSettingsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the server-wide retention policy for configuration versions.
    /// </summary>
    /// <param name="request">The retention settings update request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Updated retention settings summary.</returns>
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
