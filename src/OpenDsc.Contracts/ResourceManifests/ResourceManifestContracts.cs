// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.ResourceManifests;

/// <summary>
/// Summary of a registered DSC resource type shown in list views.
/// </summary>
public sealed class ResourceManifestSummary
{
    public required Guid Id { get; init; }
    public required string TypeName { get; init; }
    public string? Description { get; init; }
    public required string Kind { get; init; }
    public required int VersionCount { get; init; }
    public string[]? Tags { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Full details for a registered resource type, including all versions.
/// </summary>
public sealed class ResourceManifestDetails
{
    public required Guid Id { get; init; }
    public required string TypeName { get; init; }
    public string? Description { get; init; }
    public required string Kind { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
    public required IReadOnlyList<ResourceManifestVersionSummary> Versions { get; init; }
}

/// <summary>
/// Summary of a specific manifest version shown in list rows.
/// </summary>
public sealed class ResourceManifestVersionSummary
{
    public required Guid Id { get; init; }
    public required string Version { get; init; }
    public required bool IsBuiltIn { get; init; }
    public string[]? Tags { get; init; }
    public required DateTimeOffset ImportedAt { get; init; }
}

/// <summary>
/// Full details for a specific manifest version including the raw JSON and instance schema.
/// </summary>
public sealed class ResourceManifestVersionDetails
{
    public required Guid Id { get; init; }
    public required string Version { get; init; }
    public required string ManifestJson { get; init; }
    public string? InstanceSchemaJson { get; init; }
    public required bool IsBuiltIn { get; init; }
    public string[]? Tags { get; init; }
    public required DateTimeOffset ImportedAt { get; init; }
}

/// <summary>
/// Request body for manually importing a manifest from its raw JSON.
/// </summary>
public sealed class ImportResourceManifestRequest
{
    /// <summary>
    /// Raw DSC resource manifest JSON (the full .dsc.resource.json content).
    /// </summary>
    public required string ManifestJson { get; init; }
}

/// <summary>
/// Result of discovering manifests from DSC CLI with breakdown statistics.
/// </summary>
public sealed class DiscoveryResult
{
    /// <summary>Total manifests discovered from dsc resource list.</summary>
    public required int Discovered { get; init; }

    /// <summary>New manifest versions imported.</summary>
    public required int Imported { get; init; }

    /// <summary>Existing manifest versions updated.</summary>
    public required int Updated { get; init; }

    /// <summary>Manifest entries that failed to parse or process.</summary>
    public required int Failed { get; init; }
}
