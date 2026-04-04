// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Specialized;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Represents a property-level override to apply to an adapted resource manifest JSON schema.
/// </summary>
public sealed class DscPropertyOverride
{
    /// <summary>
    /// The name of the property in the embedded JSON schema to override.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// An optional replacement description for the property.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// An optional replacement title for the property.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Additional JSON schema keywords to merge into the property definition.
    /// </summary>
    public OrderedDictionary? JsonSchema { get; set; }

    /// <summary>
    /// JSON schema key names to remove from the property before merging overrides.
    /// </summary>
    public string[]? RemoveKeys { get; set; }

    /// <summary>
    /// When <c>true</c>, adds the property to the required list; when <c>false</c>, removes it.
    /// </summary>
    public bool? Required { get; set; }
}
