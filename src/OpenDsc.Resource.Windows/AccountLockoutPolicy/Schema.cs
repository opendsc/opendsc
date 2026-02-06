// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.AccountLockoutPolicy;

[Title("Account Lockout Policy Schema")]
[Description("Schema for managing Windows account lockout policy settings via OpenDsc. This is a system-wide singleton resource.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    // https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-devicelock#accountlockoutpolicy
    [Description("Number of failed logon attempts before account is locked. Use 0 to never lock out accounts.")]
    [Minimum(0)]
    [Maximum(999)]
    [Nullable(false)]
    public uint? LockoutThreshold { get; set; }

    [Description("Number of minutes a locked account remains locked before automatically unlocking. Use 0 to lock accounts until administrator unlocks. If specified, must be greater than or equal to LockoutObservationWindowMinutes.")]
    [Minimum(0)]
    [Maximum(99999)]
    [Nullable(false)]
    public uint? LockoutDurationMinutes { get; set; }

    [Description("Number of minutes after a failed logon attempt before the failed logon counter is reset to 0. If specified, must be less than or equal to LockoutDurationMinutes.")]
    [Minimum(0)]
    [Maximum(99999)]
    [Nullable(false)]
    public uint? LockoutObservationWindowMinutes { get; set; }
}
