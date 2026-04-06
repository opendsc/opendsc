// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;
using Json.Schema.Generation.Serialization;

namespace OpenDsc.Resource.Windows.AuditPolicy;

[Title("OpenDsc.Windows/AuditPolicy")]
[Description("Manage Windows audit policy for system security event auditing.")]
[AdditionalProperties(false)]
[GenerateJsonSchema]
public sealed class Schema
{
    [Required]
    [Description("The name of the audit policy subcategory to configure (e.g., 'File System', 'Logon', 'Security State Change').")]
    public string Subcategory { get; set; } = string.Empty;

    [Description("Audit setting values. Use an empty array to disable, or specify 'Success', 'Failure', or both.")]
    [Nullable(false)]
    public AuditSetting[]? Setting { get; set; }
}

[Description("Audit policy setting.")]
public enum AuditSetting
{
    [Description("Audit successful occurrences of the event type.")]
    Success,
    [Description("Audit failed attempts of the event type.")]
    Failure
}
