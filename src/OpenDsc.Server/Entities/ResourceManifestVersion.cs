// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

/// <summary>
/// A specific semver version of a registered DSC resource manifest.
/// </summary>
public sealed class ResourceManifestVersion
{
    public Guid Id { get; set; }

    public required Guid ManifestId { get; set; }

    /// <summary>
    /// Semantic version string from the manifest, e.g. "1.0.0".
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// The full raw manifest JSON as imported or discovered.
    /// </summary>
    public required string ManifestJson { get; set; }

    /// <summary>
    /// The JSON schema for a resource instance, extracted from the manifest's
    /// <c>schema.embedded</c> property (or null if schema is command-based).
    /// </summary>
    public string? InstanceSchemaJson { get; set; }

    /// <summary>
    /// JSON-encoded array of tag strings from the manifest.
    /// </summary>
    public string? TagsJson { get; set; }

    /// <summary>
    /// True when the record was auto-discovered from a local DSC installation
    /// rather than manually imported.
    /// </summary>
    public bool IsBuiltIn { get; set; }

    public DateTimeOffset ImportedAt { get; set; }

    public ResourceManifest Manifest { get; set; } = null!;
}
