// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Xml.Element;

[Title("XML Element Schema")]
[Description("Schema for managing XML element content and attributes via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The absolute file path to the XML document.")]
    public string Path { get; set; } = string.Empty;

    [Required]
    [Description("XPath expression to locate the element. Parent elements will be created recursively if they don't exist.")]
    public string XPath { get; set; } = string.Empty;

    [Description("The text content (inner text) of the element.")]
    [Nullable(false)]
    public string? Value { get; set; }

    [Description("Attributes to set on the element. When _purge is false (default), only adds/updates specified attributes. When _purge is true, removes attributes not in this dictionary.")]
    [Nullable(false)]
    public Dictionary<string, string>? Attributes { get; set; }

    [Description("Namespace prefix mappings for XPath evaluation (e.g., {'ns': 'http://example.com/schema'}).")]
    [Nullable(false)]
    public Dictionary<string, string>? Namespaces { get; set; }

    [JsonPropertyName("_purge")]
    [Description("When true, removes attributes not in the Attributes dictionary. When false, only adds/updates attributes from the Attributes dictionary without removing others. Only applicable when Attributes is specified.")]
    [Nullable(false)]
    [WriteOnly]
    [Default(false)]
    public bool? Purge { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the element exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
