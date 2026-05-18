// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Settings and retention policy operations for configurations.
/// </summary>
public interface IConfigurationSettings
{
    /// <summary>
    /// Gets the version management settings for a configuration.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Configuration settings summary, or null if the configuration is not found.</returns>
    Task<ConfigurationSettingsSummary?> GetSettingsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the version management settings for a configuration.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="request">The settings update request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Updated configuration settings summary.</returns>
    Task<ConfigurationSettingsSummary> UpdateSettingsAsync(string name, UpdateConfigurationSettingsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes custom settings for a configuration (reverts to defaults).
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteSettingsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the retention policy settings for a configuration.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Retention settings summary, or null if not found.</returns>
    Task<ConfigurationRetentionSummary?> GetRetentionSettingsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the retention policy settings for a configuration.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="request">The retention settings request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task SaveRetentionSettingsAsync(string name, SaveRetentionSettingsRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets retention settings for a configuration to the server defaults.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task ResetRetentionSettingsAsync(string name, CancellationToken cancellationToken = default);
}
