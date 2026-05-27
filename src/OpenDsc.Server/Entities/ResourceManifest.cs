// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

/// <summary>
/// A registered DSC resource type, identified by its fully qualified type name.
/// One record exists per type name; versions live in <see cref="ResourceManifestVersion"/>.
/// </summary>
public sealed class ResourceManifest
{
    public Guid Id { get; set; }

    /// <summary>
    /// Fully qualified DSC resource type name, e.g. "OpenDsc.Windows/Service".
    /// </summary>
    public required string TypeName { get; set; }

    /// <summary>
    /// Human-readable description from the manifest.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Resource kind: resource, group, adapter, importer, exporter.
    /// </summary>
    public string Kind { get; set; } = "resource";

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<ResourceManifestVersion> Versions { get; set; } = [];
}
