// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.UserRight;

[Title("Windows User Rights Assignment Schema")]
[Description("Schema for managing Windows user rights assignments.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The user rights to grant or revoke.")]
    [UniqueItems(true)]
    public UserRight[] Rights { get; set; } = [];

    [Required]
    [Description("The principal (user/group) that should have these rights. Can be specified as: username, DOMAIN\\username, SID (S-1-5-...), or UPN (user@domain.com).")]
    [Pattern(@"^(?:S-1-[0-59]-\d+(?:-\d+)*|[^@\\]+@[^@]+|CN=.+|[^\\]+\\[^\\]+|[^\\@]+)$")]
    public string Principal { get; set; } = string.Empty;

    [JsonPropertyName("_purge")]
    [Description("When true, removes all other principals from these rights. When false, only adds this principal to these rights without removing others.")]
    [Nullable(false)]
    [WriteOnly]
    [Default(false)]
    public bool? Purge { get; set; }
}
