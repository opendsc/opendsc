// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.FileSystem.File;

[Title("File Schema")]
[Description("Schema for managing files via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The path to the file.")]
    public string Path { get; set; } = string.Empty;

    [Description("The content of the file.")]
    [Nullable(false)]
    public string? Content { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the file should exist.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
