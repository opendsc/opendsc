// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class ValidationSettings
{
    public Guid Id { get; set; }

    public bool EnforceSemverCompliance { get; set; }
    public ParameterValidationMode DefaultParameterValidation { get; set; } = ParameterValidationMode.Strict;
    public bool AutoCopyParametersOnMinor { get; set; } = true;
    public bool AutoCopyParametersOnMajor { get; set; } = true;
    public bool AllowPreReleaseVersions { get; set; } = true;
    public bool RequireApprovalForPublish { get; set; }

    public bool AllowSemverComplianceOverride { get; set; } = true;
    public bool AllowParameterValidationOverride { get; set; } = true;
    public bool AllowAutoCopyOverride { get; set; } = true;
    public bool AllowPreReleaseOverride { get; set; } = true;
    public bool AllowApprovalOverride { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
