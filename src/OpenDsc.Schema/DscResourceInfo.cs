// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Schema;

/// <summary>
/// Represents a DSC resource manifest retrieved from the DSC environment.
/// Maps to the ResourceManifest schema type from dsc schema -t manifest-list.
/// </summary>
public sealed class DscResourceInfo
{
    /// <summary>
    /// Gets the fully-qualified resource type name (e.g., "OpenDsc.Windows/Service").
    /// </summary>
    public required string Type { get; set; }

    /// <summary>
    /// Gets the resource kind (resource, adapter, exporter, importer, group).
    /// </summary>
    public DscResourceKind? Kind { get; set; }

    /// <summary>
    /// Gets the human-readable description of the resource.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets the semantic version of the resource.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Gets the JSON Schema for resource instances.
    /// Defines the properties and their types that can be set on the resource.
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Gets the list of resource capabilities (operations) supported by this resource.
    /// Includes operations like get, set, test, delete, export, resolve.
    /// </summary>
    public DscCapability[]? Capabilities { get; set; }
}
