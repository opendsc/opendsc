// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

public sealed class ConfigurationSettings
{
    public required Guid ConfigurationId { get; set; }

    public bool? EnforceSemverCompliance { get; set; }
    public ParameterValidationMode? ParameterValidation { get; set; }
    public bool? AutoCopyParametersOnMinor { get; set; }
    public bool? AutoCopyParametersOnMajor { get; set; }
    public bool? AllowPreReleaseVersions { get; set; }
    public bool? RequireApprovalForPublish { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public Configuration Configuration { get; set; } = null!;
}
