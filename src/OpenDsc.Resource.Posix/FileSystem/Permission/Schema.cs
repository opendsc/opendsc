// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Posix.FileSystem.Permission;

[Title("POSIX File System Permission Schema")]
[Description("Schema for managing POSIX file and directory permissions (mode, owner, group) on Linux and macOS via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [MinLength(1)]
    [Pattern(@"^/.+")]
    [Description("The full path to the file or directory.")]
    public string Path { get; set; } = string.Empty;

    [Description("The file mode in octal notation (e.g., '0644', '0755', '644'). Accepts 3 or 4 digit octal strings with optional leading zero.")]
    [Pattern(@"^0?[0-7]{3,4}$")]
    [Nullable(false)]
    public string? Mode { get; set; }

    [Description("The owner of the file or directory. Can be specified as a username (e.g., 'root') or numeric UID (e.g., '0', '1000').")]
    [Nullable(false)]
    public string? Owner { get; set; }

    [Description("The group of the file or directory. Can be specified as a group name (e.g., 'wheel', 'staff') or numeric GID (e.g., '0', '1000').")]
    [Nullable(false)]
    public string? Group { get; set; }
}
