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
    // https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-devicelock#minimumpasswordlength
    // Legacy limit: 0-14 (can be extended to 128 with RelaxMinimumPasswordLengthLimits policy)
    [Description("Minimum number of characters required in passwords (0-14).")]
    [Minimum(0)]
    [Maximum(14)]
    [Nullable(false)]
    public uint? MinimumPasswordLength { get; set; }

    // https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-devicelock#maximumpasswordage
    // Range: 0-999 days (0 = never expire, default: 42 days)
    [Description("Maximum password age in days before password must be changed. Use 0 for unlimited (passwords never expire). Default Windows value is 42 days.")]
    [Minimum(0)]
    [Maximum(999)]
    [Nullable(false)]
    public uint? MaximumPasswordAgeDays { get; set; }

    // https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-devicelock#minimumpasswordage
    // Range: 0-998 days (default: 1 day)
    [Description("Minimum password age in days before password can be changed. Default Windows value is 0 days (passwords can be changed immediately).")]
    [Minimum(0)]
    [Maximum(998)]
    [Nullable(false)]
    public uint? MinimumPasswordAgeDays { get; set; }

    // https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-devicelock#passwordhistorysize
    // Range: 0-24 passwords (default: 24 on domain controllers, 0 on standalone servers)
    [Description("Number of unique new passwords required before an old password can be reused (0-24). Default is 0 (no history).")]
    [Minimum(0)]
    [Maximum(24)]
    [Nullable(false)]
    public uint? PasswordHistoryLength { get; set; }
}
