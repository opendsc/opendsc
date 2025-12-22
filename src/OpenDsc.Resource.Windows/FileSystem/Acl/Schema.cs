// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.FileSystem.Acl;

[Title("Windows File System Access Control List Schema")]
[Description("Schema for managing Windows file and directory permissions via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The full path to the file or directory.")]
    [Pattern(@"^[a-zA-Z]:\\(?:[^<>:""/\\|?*\x00-\x1F]+\\)*[^<>:""/\\|?*\x00-\x1F]*$")]
    public string Path { get; set; } = string.Empty;

    [Description("The owner of the file or directory. Can be specified as: username, domain\\username, or SID (S-1-5-...).")]
    [Nullable(false)]
    [Pattern(@"^(?:S-1-[0-59]-\d+(?:-\d+)*|[^\\]+\\[^\\]+|[^\\@]+)$")]
    public string? Owner { get; set; }

    [Description("The primary group of the file or directory. Can be specified as: groupName, domain\\groupName, or SID (S-1-5-...).")]
    [Nullable(false)]
    [Pattern(@"^(?:S-1-[0-59]-\d+(?:-\d+)*|[^\\]+\\[^\\]+|[^\\@]+)$")]
    public string? Group { get; set; }

    [Description("Access control entries (ACEs) to apply to the file or directory.")]
    [Nullable(false)]
    public AccessRule[]? AccessRules { get; set; }

    [JsonPropertyName("_purge")]
    [Description("When true, removes access rules not in the AccessRules list. When false, only adds rules from the AccessRules list without removing others. Only applicable when AccessRules is specified.")]
    [Nullable(false)]
    [WriteOnly]
    [Default(false)]
    public bool? Purge { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the file or directory exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
