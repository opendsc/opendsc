// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;
using Json.Schema.Generation.Serialization;

namespace OpenDsc.Resource.Windows.FileSystem.Share;

[Title("Windows SMB Share Schema")]
[Description("Schema for managing Windows SMB shares via OpenDsc.")]
[AdditionalProperties(false)]
[Id("https://opendsc.dev/schemas/v1/windows/filesystem/share.schema.json")]
[GenerateJsonSchema]
public sealed class Schema
{
    public static readonly Uri BundleUri = new("https://opendsc.dev/schemas/v1/bundled/windows/filesystem/share.schema.json");

    [Required]
    [Description("The name of the SMB share.")]
    [MinLength(1)]
    [MaxLength(80)]
    [Pattern(@"^[^\\/\t\n\r\x00-\x1f]*$")]
    public string Name { get; set; } = string.Empty;

    [Description("The local path to share. Required when creating a share.")]
    [MinLength(1)]
    [Nullable(false)]
    public string? Path { get; set; }

    [Description("The description of the share.")]
    [Nullable(false)]
    public string? Description { get; set; }

    [Description("The permissions for the share.")]
    [Nullable(false)]
    public SharePermission[]? Permissions { get; set; }

    [JsonPropertyName("_purge")]
    [Description("When true, permissions not in the array are removed. When false, only manage listed permissions.")]
    [Default(false)]
    [WriteOnly]
    [Nullable(false)]
    public bool? Purge { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the share exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
