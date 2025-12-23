// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.User;

[Title("Windows User Schema")]
[Description("Schema for managing local Windows user accounts via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The username of the local user account.")]
    [Pattern(@"^[^\x00-\x1F\\\/\[\]:;|=,+*?<>@""]{1,20}$")]
    public string UserName { get; set; } = string.Empty;

    [Description("The full name (display name) of the user.")]
    [Nullable(false)]
    public string? FullName { get; set; }

    [Description("A description of the user account.")]
    [Nullable(false)]
    public string? Description { get; set; }

    [Description("The password for the user account. Write-only, not returned by Get operation.")]
    [Nullable(false)]
    [WriteOnly]
    public string? Password { get; set; }

    [Description("Whether the user account is disabled.")]
    [Nullable(false)]
    public bool? Disabled { get; set; }

    [Description("Whether the user's password never expires.")]
    [Nullable(false)]
    public bool? PasswordNeverExpires { get; set; }

    [Description("Whether the user may change their own password.")]
    [Nullable(false)]
    public bool? UserMayNotChangePassword { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the user account exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
