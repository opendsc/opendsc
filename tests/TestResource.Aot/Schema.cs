// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;
using Json.Schema.Generation.Serialization;

namespace TestResource.Aot;

[Title("Test Resource Schema")]
[Description("Schema for testing AOT code generation in OpenDsc.")]
[AdditionalProperties(false)]
[GenerateJsonSchema]
public sealed class Schema
{
    [Required]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("_exist")]
    [Default(true)]
    [Nullable(false)]
    public bool? Exist { get; set; }

    [JsonPropertyName("_inDesiredState")]
    public bool? InDesiredState { get; set; }
}
