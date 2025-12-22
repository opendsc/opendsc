// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.Group;

[Title("Windows Group Schema")]
[Description("Schema for managing local Windows groups via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The name of the local group.")]
    [Pattern(@"^[^\x00-\x1F\\\/\[\]:;|=,+*?<>@""]{1,256}$")]
    public string GroupName { get; set; } = string.Empty;

    [Description("A description of the group.")]
    [Nullable(false)]
    public string? Description { get; set; }

    [Description("List of group members. Members can be specified as: username, domain\\username, SID (S-1-5-...), UPN (user@domain), or DN (CN=user,DC=...).")]
    [Nullable(false)]
    [Pattern(@"^(?:S-1-[0-59]-\d+(?:-\d+)*|[^@\\]+@[^@]+|CN=.+|[^\\]+\\[^\\]+|[^\\@]+)$", GenericParameter = 0)]
    public string[]? Members { get; set; }

    [JsonPropertyName("_purge")]
    [Description("When true, removes members not in the Members list. When false, only adds members from the Members list without removing others. Only applicable when Members is specified.")]
    [Nullable(false)]
    [WriteOnly]
    public bool? Purge { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the group exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
