// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Configurations;

namespace OpenDsc.Contracts.Settings;

/// <summary>
/// Server settings summary.
/// </summary>
public sealed class ServerSettingsSummary
{
    /// <summary>
    /// How often nodes should rotate their certificates (informational).
    /// </summary>
    public TimeSpan CertificateRotationInterval { get; set; }

    /// <summary>
    /// Multiplier applied to a node's ConfigurationModeInterval to determine staleness threshold.
    /// </summary>
    public double StalenessMultiplier { get; set; }
}

/// <summary>
/// Request to update server settings.
/// </summary>
public sealed class UpdateServerSettingsRequest
{
    /// <summary>
    /// How often nodes should rotate their certificates (informational).
    /// </summary>
    public TimeSpan? CertificateRotationInterval { get; set; }

    /// <summary>
    /// Multiplier applied to a node's ConfigurationModeInterval to determine staleness threshold.
    /// </summary>
    public double? StalenessMultiplier { get; set; }
}

/// <summary>
/// Server-wide LCM default settings summary.
/// </summary>
public sealed class ServerLcmDefaultsSummary
{
    /// <summary>
    /// Server-wide default LCM operating mode. Null means no server default is set.
    /// </summary>
    public ConfigurationMode? DefaultConfigurationMode { get; set; }

    /// <summary>
    /// Server-wide default LCM configuration mode interval. Null means no server default is set.
    /// </summary>
    public TimeSpan? DefaultConfigurationModeInterval { get; set; }

    /// <summary>
    /// Server-wide default compliance reporting setting. Null means no server default is set.
    /// </summary>
    public bool? DefaultReportCompliance { get; set; }
}

/// <summary>
/// Request to update server-wide LCM default settings. Null values clear the corresponding default.
/// </summary>
public sealed class UpdateServerLcmDefaultsRequest
{
    /// <summary>
    /// Server-wide default LCM operating mode. Set to null to clear the default.
    /// </summary>
    public ConfigurationMode? DefaultConfigurationMode { get; set; }

    /// <summary>
    /// Server-wide default LCM configuration mode interval. Set to null to clear the default.
    /// </summary>
    public TimeSpan? DefaultConfigurationModeInterval { get; set; }

    /// <summary>
    /// Server-wide default compliance reporting setting. Set to null to clear the default.
    /// </summary>
    public bool? DefaultReportCompliance { get; set; }
}

/// <summary>
/// Validation settings summary.
/// </summary>
public sealed class ValidationSettingsSummary
{
    /// <summary>
    /// Whether configuration versions must use semantic versioning.
    /// </summary>
    public bool RequireSemVer { get; set; }

    /// <summary>
    /// Default parameter validation mode applied to configurations.
    /// </summary>
    public ParameterValidationMode DefaultParameterValidationMode { get; set; }

    /// <summary>
    /// Whether individual configurations are allowed to override the default validation mode.
    /// </summary>
    public bool AllowConfigurationOverride { get; set; }

    /// <summary>
    /// Whether individual parameters are allowed to override the default validation mode.
    /// </summary>
    public bool AllowParameterValidationOverride { get; set; }
}

/// <summary>
/// Request to update validation settings.
/// </summary>
public sealed class UpdateValidationSettingsRequest
{
    /// <summary>
    /// Whether configuration versions must use semantic versioning. Null leaves the existing value unchanged.
    /// </summary>
    public bool? RequireSemVer { get; set; }

    /// <summary>
    /// Default parameter validation mode. Null leaves the existing value unchanged.
    /// </summary>
    public ParameterValidationMode? DefaultParameterValidationMode { get; set; }

    /// <summary>
    /// Whether individual configurations may override the default validation mode. Null leaves the existing value unchanged.
    /// </summary>
    public bool? AllowConfigurationOverride { get; set; }

    /// <summary>
    /// Whether individual parameters may override the default validation mode. Null leaves the existing value unchanged.
    /// </summary>
    public bool? AllowParameterValidationOverride { get; set; }
}

/// <summary>
/// Global retention policy settings.
/// </summary>
public sealed class RetentionSettingsSummary
{
    /// <summary>
    /// Whether automatic retention policy enforcement is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Maximum number of non-release versions to retain per configuration.
    /// </summary>
    public int KeepVersions { get; set; }

    /// <summary>
    /// Maximum age in days for non-release versions.
    /// </summary>
    public int KeepDays { get; set; }

    /// <summary>
    /// Whether release versions are exempt from retention pruning.
    /// </summary>
    public bool KeepReleaseVersions { get; set; }

    /// <summary>
    /// How often (in hours) the scheduled retention job runs.
    /// </summary>
    public int ScheduleIntervalHours { get; set; }

    /// <summary>
    /// Maximum number of compliance reports to retain per node.
    /// </summary>
    public int ReportKeepCount { get; set; }

    /// <summary>
    /// Maximum age in days for compliance reports.
    /// </summary>
    public int ReportKeepDays { get; set; }

    /// <summary>
    /// Maximum number of LCM status events to retain per node.
    /// </summary>
    public int StatusEventKeepCount { get; set; }

    /// <summary>
    /// Maximum age in days for LCM status events.
    /// </summary>
    public int StatusEventKeepDays { get; set; }
}

/// <summary>
/// Request to update global retention policy settings. Null fields leave existing values unchanged.
/// </summary>
public sealed class UpdateRetentionSettingsRequest
{
    /// <summary>
    /// Whether automatic retention policy enforcement is enabled. Null leaves the existing value unchanged.
    /// </summary>
    public bool? Enabled { get; set; }

    /// <summary>
    /// Maximum number of non-release versions to retain per configuration. Null leaves the existing value unchanged.
    /// </summary>
    public int? KeepVersions { get; set; }

    /// <summary>
    /// Maximum age in days for non-release versions. Null leaves the existing value unchanged.
    /// </summary>
    public int? KeepDays { get; set; }

    /// <summary>
    /// Whether release versions are exempt from retention pruning. Null leaves the existing value unchanged.
    /// </summary>
    public bool? KeepReleaseVersions { get; set; }

    /// <summary>
    /// How often (in hours) the scheduled retention job runs. Null leaves the existing value unchanged.
    /// </summary>
    public int? ScheduleIntervalHours { get; set; }

    /// <summary>
    /// Maximum number of compliance reports to retain per node. Null leaves the existing value unchanged.
    /// </summary>
    public int? ReportKeepCount { get; set; }

    /// <summary>
    /// Maximum age in days for compliance reports. Null leaves the existing value unchanged.
    /// </summary>
    public int? ReportKeepDays { get; set; }

    /// <summary>
    /// Maximum number of LCM status events to retain per node. Null leaves the existing value unchanged.
    /// </summary>
    public int? StatusEventKeepCount { get; set; }

    /// <summary>
    /// Maximum age in days for LCM status events. Null leaves the existing value unchanged.
    /// </summary>
    public int? StatusEventKeepDays { get; set; }
}

/// <summary>
/// Standard error response.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// Error message.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}
