// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Specialized;
using System.Text.Json;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Represents a DSCv3 resource manifest list bundling adapted resources, command resources, and extensions.
/// </summary>
public sealed class DscResourceManifestList
{
    /// <summary>
    /// The collection of adapted resource manifests serialized as ordered dictionaries.
    /// </summary>
    public List<OrderedDictionary> AdaptedResources { get; } = [];

    /// <summary>
    /// The collection of command-based resource manifests.
    /// </summary>
    public List<OrderedDictionary> Resources { get; } = [];

    /// <summary>
    /// The collection of extension manifests.
    /// </summary>
    public List<OrderedDictionary> Extensions { get; } = [];

    /// <summary>
    /// Adds an adapted resource manifest to the list.
    /// </summary>
    /// <param name="manifest">The adapted resource manifest to add.</param>
    public void AddAdaptedResource(DscAdaptedResourceManifest manifest) =>
        AdaptedResources.Add(manifest.ToHashtable());

    /// <summary>
    /// Adds a command-based resource manifest to the list.
    /// </summary>
    /// <param name="resource">The resource manifest as an ordered dictionary.</param>
    public void AddResource(OrderedDictionary resource) =>
        Resources.Add(resource);

    /// <summary>
    /// Adds an extension manifest to the list.
    /// </summary>
    /// <param name="extension">The extension manifest as an ordered dictionary.</param>
    public void AddExtension(OrderedDictionary extension) =>
        Extensions.Add(extension);

    /// <summary>
    /// Serializes the manifest list to a JSON string, including only non-empty sections.
    /// </summary>
    /// <returns>The JSON representation of the manifest list.</returns>
    public string ToJson()
    {
        var result = new OrderedDictionary();

        if (AdaptedResources.Count > 0)
        {
            result["adaptedResources"] = AdaptedResources;
        }

        if (Resources.Count > 0)
        {
            result["resources"] = Resources;
        }

        if (Extensions.Count > 0)
        {
            result["extensions"] = Extensions;
        }

        return JsonSerializer.Serialize(result, ManifestJsonSerializer.Options);
    }
}
