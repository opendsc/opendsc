// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.ResourceManifests;

namespace OpenDsc.Server.Services;

public interface IResourceManifestService
{
    Task<IReadOnlyList<ResourceManifestSummary>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResourceManifestDetails?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResourceManifestDetails?> GetByTypeNameAsync(string typeName, CancellationToken cancellationToken = default);
    Task<ResourceManifestVersionDetails?> GetVersionDetailsAsync(Guid manifestId, string version, CancellationToken cancellationToken = default);
    Task<ResourceManifestVersionDetails?> GetVersionDetailsByIdAsync(Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports a manifest from its raw JSON. Creates a new type record if not already present,
    /// or adds a new version record if the type exists but the version is new.
    /// </summary>
    Task<ResourceManifestVersionDetails> ImportAsync(string manifestJson, bool isBuiltIn = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Imports one or more manifests from text content. Accepts:
    /// single manifest JSON, JSON array of manifests, or YAML
    /// (single mapping or sequence). Returns the count of versions imported.
    /// </summary>
    Task<int> ImportManyAsync(string content, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discovers DSC resources using the MCP server and upserts each discovered manifest.
    /// Returns statistics about discovered, imported, updated, and skipped manifests.
    /// Discovery operations are serialized - only one can run at a time across all users/requests.
    /// </summary>
    Task<DiscoveryResult> DiscoverFromMcpAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the timestamp of the last successful resource discovery, or null if discovery has not run.
    /// </summary>
    DateTimeOffset? GetLastDiscoveryTime { get; }

    /// <summary>
    /// Gets the JSON schema for a specific resource type from an imported manifest.
    /// Returns the schema JSON if available, or null if the resource is not found or has no schema.
    /// </summary>
    Task<string?> GetResourceSchemaAsync(string typeName, CancellationToken cancellationToken = default);

    Task DeleteManifestAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeleteVersionAsync(Guid versionId, CancellationToken cancellationToken = default);
}
