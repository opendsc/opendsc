// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.FileSystem.Directory;

[Title("Directory Schema")]
[Description("Schema for managing directories via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The path to the directory.")]
    public string Path { get; set; } = string.Empty;

    [Description("The path to the source directory to copy contents from.")]
    [Nullable(false)]
    public string? SourcePath { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the directory should exist.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }

    [JsonPropertyName("_inDesiredState")]
    [Description("Indicates whether the instance is in desired state.")]
    [ReadOnly]
    public bool? InDesiredState { get; set; }
}
