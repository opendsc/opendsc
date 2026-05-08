// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Read operations for configurations.
/// </summary>
public interface IConfigurationReader
{
    Task<List<ConfigurationSummary>> GetConfigurationsAsync(CancellationToken cancellationToken = default);
    Task<ConfigurationDetails?> GetConfigurationAsync(string name, CancellationToken cancellationToken = default);
    Task<List<ConfigurationVersionDetails>?> GetVersionsAsync(string name, CancellationToken cancellationToken = default);
    Task<List<string>> GetConfigurationVersionListAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> IsConfigurationAssignedAsync(string name, CancellationToken cancellationToken = default);
    Task<VersionUsageInfo> IsVersionInUseAsync(string name, string version, CancellationToken cancellationToken = default);
    Task<Guid?> GetParameterSchemaIdAsync(string name, CancellationToken cancellationToken = default);
}
