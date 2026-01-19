// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Json.Value;

[Title("JSON Value Schema")]
[Description("Schema for managing JSON values at specified JSONPath locations via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The absolute file path to the JSON document.")]
    public string Path { get; set; } = string.Empty;

    [Required]
    [Description("JSONPath expression to locate the value (must start with '$'). Parent paths will be created recursively if they don't exist.")]
    [Pattern(@"^\$")]
    public string JsonPath { get; set; } = string.Empty;

    [Description("The JSON value to set. Can be a string, number, boolean, null, object, or array.")]
    public JsonElement? Value { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the value exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
