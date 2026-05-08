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
    Task<CompositeConfigurationDetails> CreateAsync(CreateCompositeConfigurationRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string name, CancellationToken cancellationToken = default);
    Task<CompositeConfigurationVersionDetails> CreateVersionAsync(string name, CreateCompositeConfigurationVersionRequest request, CancellationToken cancellationToken = default);
    Task CreateVersionFromExistingAsync(string name, CreateCompositeVersionFromExistingRequest request, CancellationToken cancellationToken = default);
    Task PublishVersionAsync(string name, string version, CancellationToken cancellationToken = default);
    Task DeleteVersionAsync(string name, string version, CancellationToken cancellationToken = default);
    Task<CompositeConfigurationItemDetails> AddChildAsync(string name, string version, AddChildConfigurationRequest request, CancellationToken cancellationToken = default);
    Task<CompositeConfigurationItemDetails> UpdateChildAsync(Guid itemId, UpdateChildConfigurationRequest request, CancellationToken cancellationToken = default);
    Task RemoveChildAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task ReorderChildAsync(Guid itemId, int newOrder, CancellationToken cancellationToken = default);
}
