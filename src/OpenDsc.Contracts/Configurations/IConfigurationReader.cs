// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Read operations for configurations.
/// </summary>
public interface IConfigurationReader
{
    /// <summary>
    /// Gets a list of all configurations.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of configuration summaries.</returns>
    Task<IReadOnlyList<ConfigurationSummary>> GetConfigurationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets detailed information for a specific configuration.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed configuration information, or null if not found.</returns>
    Task<ConfigurationDetails?> GetConfigurationAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all published versions of a configuration.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of version details, or null if the configuration is not found.</returns>
    Task<IReadOnlyList<ConfigurationVersionDetails>?> GetVersionsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of all published version identifiers for a configuration.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A list of version identifiers (e.g., "1.0.0", "1.1.0").</returns>
    Task<IReadOnlyList<string>> GetConfigurationVersionListAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a configuration has been assigned to any node.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>True if the configuration is assigned to at least one node; otherwise, false.</returns>
    Task<bool> IsConfigurationAssignedAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether a specific configuration version is in use by any node.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="version">The version identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Usage information including the number of nodes using the version.</returns>
    Task<VersionUsageInfo> IsVersionInUseAsync(string name, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the parameter schema identifier for a configuration, if defined.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The schema identifier, or null if no schema is defined.</returns>
    Task<Guid?> GetParameterSchemaIdAsync(string name, CancellationToken cancellationToken = default);
}
