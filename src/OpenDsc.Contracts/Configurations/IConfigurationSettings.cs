// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Settings and retention policy operations for configurations.
/// </summary>
public interface IConfigurationSettings
{
    Task<ConfigurationSettingsSummary?> GetSettingsAsync(string name, CancellationToken cancellationToken = default);
    Task<ConfigurationSettingsSummary> UpdateSettingsAsync(string name, UpdateConfigurationSettingsRequest request, CancellationToken cancellationToken = default);
    Task DeleteSettingsAsync(string name, CancellationToken cancellationToken = default);
    Task<ConfigurationRetentionSummary?> GetRetentionSettingsAsync(string name, CancellationToken cancellationToken = default);
    Task SaveRetentionSettingsAsync(string name, SaveRetentionSettingsRequest request, CancellationToken cancellationToken = default);
    Task ResetRetentionSettingsAsync(string name, CancellationToken cancellationToken = default);
}
