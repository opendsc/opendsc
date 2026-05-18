// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.Configurations;

namespace OpenDsc.Contracts.CompositeConfigurations;

/// <summary>
/// Create, update, and delete operations for composite configurations and their versions.
/// </summary>
public interface ICompositeConfigurationManager
{
    /// <summary>
    /// Creates a new composite configuration with an initial version.
    /// </summary>
    /// <param name="request">The composite configuration creation request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information about the created composite configuration.</returns>
    Task<CompositeConfigurationDetails> CreateAsync(CreateCompositeConfigurationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an existing composite configuration and all its versions.
    /// </summary>
    /// <param name="name">The name of the composite configuration to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new version of an existing composite configuration.
    /// </summary>
    /// <param name="name">The name of the composite configuration.</param>
    /// <param name="request">The version creation request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information about the created version.</returns>
    Task<CompositeConfigurationVersionDetails> CreateVersionAsync(string name, CreateCompositeConfigurationVersionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new version of a composite configuration by copying children from an existing version.
    /// </summary>
    /// <param name="name">The name of the composite configuration.</param>
    /// <param name="request">The request specifying source version and new version identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task CreateVersionFromExistingAsync(string name, CreateCompositeVersionFromExistingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a draft composite version, making it available for node assignment.
    /// </summary>
    /// <param name="name">The name of the composite configuration.</param>
    /// <param name="version">The version identifier to publish.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task PublishVersionAsync(string name, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a specific version of a composite configuration.
    /// </summary>
    /// <param name="name">The name of the composite configuration.</param>
    /// <param name="version">The version identifier to delete.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteVersionAsync(string name, string version, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a child configuration to a composite version.
    /// </summary>
    /// <param name="name">The name of the composite configuration.</param>
    /// <param name="version">The version identifier.</param>
    /// <param name="request">The request containing child configuration details and ordering.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Detailed information about the added child configuration item.</returns>
    Task<CompositeConfigurationItemDetails> AddChildAsync(string name, string version, AddChildConfigurationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a child configuration's settings within a composite version.
    /// </summary>
    /// <param name="itemId">The unique identifier of the composite item to update.</param>
    /// <param name="request">The request containing updated child configuration settings.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Updated detailed information about the child configuration item.</returns>
    Task<CompositeConfigurationItemDetails> UpdateChildAsync(Guid itemId, UpdateChildConfigurationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a child configuration from a composite version.
    /// </summary>
    /// <param name="itemId">The unique identifier of the composite item to remove.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task RemoveChildAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the execution order of a child configuration in the composite.
    /// </summary>
    /// <param name="itemId">The unique identifier of the composite item to reorder.</param>
    /// <param name="newOrder">The new order/sequence number.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task ReorderChildAsync(Guid itemId, int newOrder, CancellationToken cancellationToken = default);
}
