// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.AuditPolicy;

[Title("OpenDsc.Windows/AuditPolicy")]
[Description("Manage Windows audit policy for system security event auditing.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The name of the audit policy subcategory to configure (e.g., 'File System', 'Logon', 'Security State Change').")]
    public string Subcategory { get; set; } = string.Empty;

    [Description("Audit policy settings for the subcategory. Use 'None' to disable, or combine 'Success' and 'Failure' flags.")]
    [Nullable(false)]
    public AuditSetting? Setting { get; set; }
}

[Flags]
[Description("Audit policy setting flags.")]
public enum AuditSetting
{
    [Description("Do not audit the event type.")]
    None = 0,
    [Description("Audit successful occurrences of the event type.")]
    Success = 1,
    [Description("Audit failed attempts of the event type.")]
    Failure = 2,
    [Description("Audit both successful and failed attempts of the event type.")]
    SuccessAndFailure = 3
}
