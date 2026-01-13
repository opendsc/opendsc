// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.FileSystem.SymbolicLink;

[Title("Symbolic Link Schema")]
[Description("Schema for managing symbolic links via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The path where the symbolic link should be created.")]
    public string Path { get; set; } = string.Empty;

    [Required]
    [Description("The target path that the symbolic link points to.")]
    public string Target { get; set; } = string.Empty;

    [Description("The type of the symbolic link target. If not specified, it will be auto-detected.")]
    [Nullable(false)]
    public LinkType? Type { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the symbolic link should exist.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}

public enum LinkType
{
    Directory,
    File
}
