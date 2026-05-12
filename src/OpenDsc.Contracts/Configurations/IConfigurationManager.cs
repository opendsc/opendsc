// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// Create, update, and delete operations for configurations and their versions.
/// </summary>
public interface IConfigurationManager
{
    Task<ConfigurationDetails> CreateAsync(CreateConfigurationAdminRequest request, CancellationToken cancellationToken = default);
    Task<ConfigurationDetails> UpdateAsync(string name, UpdateConfigurationAdminRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string name, CancellationToken cancellationToken = default);
    Task<ConfigurationVersionDetails> CreateVersionAsync(string name, CreateConfigurationVersionRequest request, CancellationToken cancellationToken = default);
    Task<ConfigurationVersionDetails> CreateVersionFromExistingAsync(string name, CreateVersionFromExistingRequest request, CancellationToken cancellationToken = default);
    Task<PublishResult> PublishVersionAsync(string name, string version, CancellationToken cancellationToken = default);
    Task DeleteVersionAsync(string name, string version, CancellationToken cancellationToken = default);
}
