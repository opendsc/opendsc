// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Create, update, and delete operations for configurations and their versions.
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Creates a new configuration with an initial version.
    /// </summary>
    /// <param name="request">The configuration creation request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information about the created configuration.</returns>
    Task<ConfigurationDetails> CreateAsync(CreateConfigurationAdminRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing configuration's metadata.
    /// </summary>
    /// <param name="name">The name of the configuration to update.</param>
    /// <param name="request">The configuration update request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Updated detailed information about the configuration.</returns>
    Task<ConfigurationDetails> UpdateAsync(string name, UpdateConfigurationAdminRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing configuration and all its versions.
    /// </summary>
    /// <param name="name">The name of the configuration to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new version of an existing configuration from scratch.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="request">The version creation request containing files.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information about the created version.</returns>
    Task<ConfigurationVersionDetails> CreateVersionAsync(string name, CreateConfigurationVersionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new version of a configuration by copying files from an existing version.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="request">The request specifying source version and new version identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information about the created version.</returns>
    Task<ConfigurationVersionDetails> CreateVersionFromExistingAsync(string name, CreateVersionFromExistingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a draft configuration version, making it available for node assignment.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="version">The version identifier to publish.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about the publish operation, including any validation results.</returns>
    Task<PublishResult> PublishVersionAsync(string name, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific version of a configuration.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="version">The version identifier to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteVersionAsync(string name, string version, CancellationToken cancellationToken = default);
}
