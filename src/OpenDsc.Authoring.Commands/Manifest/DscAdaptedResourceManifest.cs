// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Specialized;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Represents a DSCv3 adapted resource manifest describing a class-based PowerShell DSC resource.
/// </summary>
public sealed class DscAdaptedResourceManifest
{
    /// <summary>
    /// The JSON schema URI for this manifest format.
    /// </summary>
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = string.Empty;

    /// <summary>
    /// The fully qualified resource type name (e.g. <c>ModuleName/ResourceName</c>).
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The resource kind. Defaults to <c>resource</c>.
    /// </summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "resource";

    /// <summary>
    /// The semantic version of the resource.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// The list of capabilities the resource supports (e.g. <c>get</c>, <c>set</c>, <c>test</c>).
    /// </summary>
    [JsonPropertyName("capabilities")]
    public string[] Capabilities { get; set; } = [];

    /// <summary>
    /// A human-readable description of the resource.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The author of the resource.
    /// </summary>
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// The adapter required to invoke this resource.
    /// </summary>
    [JsonPropertyName("requireAdapter")]
    public string RequireAdapter { get; set; } = string.Empty;

    /// <summary>
    /// The module-relative path to the script or module file containing the resource.
    /// </summary>
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// The wrapper containing the embedded JSON schema for the resource properties.
    /// </summary>
    [JsonPropertyName("schema")]
    public DscAdaptedResourceManifestSchemaWrapper ManifestSchema { get; set; } = new();

    /// <summary>
    /// Serializes the manifest to a JSON string with ordered keys.
    /// </summary>
    /// <returns>The JSON representation of the manifest.</returns>
    public string ToJson()
    {
        var manifest = new OrderedDictionary
        {
            ["$schema"] = Schema,
            ["type"] = Type,
            ["kind"] = Kind,
            ["version"] = Version,
            ["capabilities"] = Capabilities,
            ["description"] = Description,
            ["author"] = Author,
            ["requireAdapter"] = RequireAdapter,
            ["path"] = Path,
            ["schema"] = new OrderedDictionary
            {
                ["embedded"] = ManifestSchema.Embedded,
            },
        };

        return JsonSerializer.Serialize(manifest, ManifestJsonSerializer.Options);
    }

    internal OrderedDictionary ToHashtable()
    {
        return new OrderedDictionary
        {
            ["$schema"] = Schema,
            ["type"] = Type,
            ["kind"] = Kind,
            ["version"] = Version,
            ["capabilities"] = Capabilities,
            ["description"] = Description,
            ["author"] = Author,
            ["requireAdapter"] = RequireAdapter,
            ["path"] = Path,
            ["schema"] = new OrderedDictionary
            {
                ["embedded"] = ManifestSchema.Embedded,
            },
        };
    }
}

/// <summary>
/// Wraps the embedded JSON schema section of an adapted resource manifest.
/// </summary>
public sealed class DscAdaptedResourceManifestSchemaWrapper
{
    /// <summary>
    /// The embedded JSON schema describing the resource properties.
    /// </summary>
    [JsonPropertyName("embedded")]
    public OrderedDictionary Embedded { get; set; } = [];
}
