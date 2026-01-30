// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.PasswordPolicy;

[Title("Password Policy Schema")]
[Description("Schema for managing Windows password policy settings via OpenDsc. This is a system-wide singleton resource.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Description("Minimum number of characters required in passwords (0-14).")]
    [Minimum(0)]
    [Maximum(14)]
    [Nullable(false)]
    public uint? MinimumPasswordLength { get; set; }

    [Description("Maximum password age in days before password must be changed. Use 0 for unlimited (passwords never expire). Default Windows value is 42 days.")]
    [Minimum(0)]
    [Maximum(49710)]
    [Nullable(false)]
    public uint? MaximumPasswordAgeDays { get; set; }

    [Description("Minimum password age in days before password can be changed. Default Windows value is 0 days (passwords can be changed immediately).")]
    [Minimum(0)]
    [Maximum(49710)]
    [Nullable(false)]
    public uint? MinimumPasswordAgeDays { get; set; }

    [Description("Number of unique new passwords required before an old password can be reused (0-24). Default is 0 (no history).")]
    [Minimum(0)]
    [Maximum(24)]
    [Nullable(false)]
    public uint? PasswordHistoryLength { get; set; }
}
