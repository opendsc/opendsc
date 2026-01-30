// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.AuditPolicy;

[Title("OpenDsc.Windows/AuditPolicy")]
[Description("Manage Windows audit policy for system security event auditing.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The GUID of the audit policy subcategory to configure. Use well-known GUIDs from auditing constants (e.g., '0cce921d-69ae-11d9-bed3-505054503030' for FileSystem access).")]
    [Pattern(@"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$")]
    public string SubcategoryGuid { get; set; } = string.Empty;

    [Description("Audit policy setting for the subcategory.")]
    [Nullable(false)]
    public AuditSetting? Setting { get; set; }

    [JsonPropertyName("_exist")]
    [Description("When false, audit policy for the subcategory is reset to 'None'. When true or omitted, applies the specified Setting.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}

[Description("Audit policy setting specifying what types of events to audit.")]
public enum AuditSetting
{
    [Description("Do not audit the event type.")]
    None,
    [Description("Audit successful occurrences of the event type.")]
    Success,
    [Description("Audit failed attempts of the event type.")]
    Failure,
    [Description("Audit both successful and failed attempts of the event type.")]
    SuccessAndFailure
}
