// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.CompositeConfigurations;

/// <summary>
/// Read operations for composite configurations.
/// </summary>
public interface ICompositeConfigurationReader
{
    /// <summary>
    /// Gets a list of all composite configurations on the server.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of composite configuration summaries.</returns>
    Task<IReadOnlyList<CompositeConfigurationSummary>> GetCompositeConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information for a specific composite configuration.
    /// </summary>
    /// <param name="name">The name of the composite configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information about the composite configuration, or null if not found.</returns>
    Task<CompositeConfigurationDetails?> GetCompositeConfigurationAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all versions of a composite configuration.
    /// </summary>
    /// <param name="name">The name of the composite configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of version details, or null if the composite is not found.</returns>
    Task<IReadOnlyList<CompositeConfigurationVersionDetails>?> GetVersionsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific version of a composite configuration.
    /// </summary>
    /// <param name="name">The name of the composite configuration.</param>
    /// <param name="version">The version identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Details about the composite version, or null if not found.</returns>
    Task<CompositeConfigurationVersionDetails?> GetVersionAsync(string name, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of child configurations available for adding to a composite.
    /// </summary>
    /// <param name="excludeIds">Configuration IDs to exclude from the results (e.g., the composite itself).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of available child configuration options with their available major versions.</returns>
    Task<IReadOnlyList<ChildConfigurationOption>> GetAvailableChildConfigurationsAsync(IEnumerable<Guid> excludeIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the available major versions for a specific configuration that can be added as a child.
    /// </summary>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of available major version numbers.</returns>
    Task<IReadOnlyList<int>> GetAvailableMajorVersionsAsync(Guid configurationId, CancellationToken cancellationToken = default);
}
