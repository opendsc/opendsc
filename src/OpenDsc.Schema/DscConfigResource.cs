// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace OpenDsc.Schema;

/// <summary>
/// A resource instance declared inside a DSC v3 configuration document.
/// </summary>
public sealed class DscConfigResource
{
    /// <summary>
    /// The fully qualified resource type name (e.g. <c>PSDesiredStateConfiguration/File</c>).
    /// </summary>
    [JsonRequired]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The unique name of this resource instance within the configuration.
    /// </summary>
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The desired-state properties for the resource.
    /// Values are arbitrary JSON and represented as <see cref="JsonNode"/> for AOT-safe serialization.
    /// </summary>
    [JsonRequired]
    public IReadOnlyDictionary<string, JsonNode?> Properties { get; set; } = new Dictionary<string, JsonNode?>();

    /// <summary>
    /// Dependencies on other resource instances expressed as <c>resourceId()</c> expressions,
    /// e.g. <c>[resourceId('PSDesiredStateConfiguration/File', 'CopyHosts')]</c>.
    /// </summary>
    public IReadOnlyList<string>? DependsOn { get; set; }
}
