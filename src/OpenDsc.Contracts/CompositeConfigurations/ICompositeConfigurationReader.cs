// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.CompositeConfigurations;

/// <summary>
/// Read operations for composite configurations.
/// </summary>
public interface ICompositeConfigurationReader
{
    Task<List<CompositeConfigurationSummary>> GetCompositeConfigurationsAsync(CancellationToken cancellationToken = default);
    Task<CompositeConfigurationDetails?> GetCompositeConfigurationAsync(string name, CancellationToken cancellationToken = default);
    Task<List<CompositeConfigurationVersionDetails>?> GetVersionsAsync(string name, CancellationToken cancellationToken = default);
    Task<CompositeConfigurationVersionDetails?> GetVersionAsync(string name, string version, CancellationToken cancellationToken = default);
    Task<List<ChildConfigurationOption>> GetAvailableChildConfigurationsAsync(IEnumerable<Guid> excludeIds, CancellationToken cancellationToken = default);
    Task<List<int>> GetAvailableMajorVersionsAsync(Guid configurationId, CancellationToken cancellationToken = default);
}
