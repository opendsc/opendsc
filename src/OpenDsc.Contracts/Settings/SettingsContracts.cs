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
    public TimeSpan CertificateRotationInterval { get; internal set; }

    /// <summary>
    /// Multiplier applied to a node's ConfigurationModeInterval to determine staleness threshold.
    /// </summary>
    public double StalenessMultiplier { get; internal set; }
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
    public ConfigurationMode? DefaultConfigurationMode { get; internal set; }

    /// <summary>
    /// Server-wide default LCM configuration mode interval. Null means no server default is set.
    /// </summary>
    public TimeSpan? DefaultConfigurationModeInterval { get; internal set; }

    /// <summary>
    /// Server-wide default compliance reporting setting. Null means no server default is set.
    /// </summary>
    public bool? DefaultReportCompliance { get; internal set; }
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
    public bool RequireSemVer { get; internal set; }
    public ParameterValidationMode DefaultParameterValidationMode { get; internal set; }
    public bool AllowConfigurationOverride { get; internal set; }
    public bool AllowParameterValidationOverride { get; internal set; }
}

/// <summary>
/// Request to update validation settings.
/// </summary>
public sealed class UpdateValidationSettingsRequest
{
    public bool? RequireSemVer { get; set; }
    public ParameterValidationMode? DefaultParameterValidationMode { get; set; }
    public bool? AllowConfigurationOverride { get; set; }
    public bool? AllowParameterValidationOverride { get; set; }
}

/// <summary>
/// Global retention policy settings.
/// </summary>
public sealed class RetentionSettingsSummary
{
    public bool Enabled { get; internal set; }
    public int KeepVersions { get; internal set; }
    public int KeepDays { get; internal set; }
    public bool KeepReleaseVersions { get; internal set; }
    public int ScheduleIntervalHours { get; internal set; }
    public int ReportKeepCount { get; internal set; }
    public int ReportKeepDays { get; internal set; }
    public int StatusEventKeepCount { get; internal set; }
    public int StatusEventKeepDays { get; internal set; }
}

/// <summary>
/// Request to update global retention policy settings. Null fields leave existing values unchanged.
/// </summary>
public sealed class UpdateRetentionSettingsRequest
{
    public bool? Enabled { get; set; }
    public int? KeepVersions { get; set; }
    public int? KeepDays { get; set; }
    public bool? KeepReleaseVersions { get; set; }
    public int? ScheduleIntervalHours { get; set; }
    public int? ReportKeepCount { get; set; }
    public int? ReportKeepDays { get; set; }
    public int? StatusEventKeepCount { get; set; }
    public int? StatusEventKeepDays { get; set; }
}

/// <summary>
/// Summary of a retention cleanup run.
/// </summary>
public sealed class RetentionRunSummary
{
    public Guid Id { get; internal set; }
    public DateTimeOffset StartedAt { get; internal set; }
    public DateTimeOffset? CompletedAt { get; internal set; }
    public string VersionType { get; internal set; } = string.Empty;
    public bool IsScheduled { get; internal set; }
    public bool IsDryRun { get; internal set; }
    public int DeletedCount { get; internal set; }
    public int KeptCount { get; internal set; }
    public string? Error { get; internal set; }
}

/// <summary>
/// Standard error response.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>
    /// Error message.
    /// </summary>
    public string Error { get; internal set; } = string.Empty;
}
