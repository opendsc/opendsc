// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Configurations;

namespace OpenDsc.Contracts.Settings;

/// <summary>
/// Server settings response.
/// </summary>
public sealed class ServerSettingsResponse
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
/// Server-wide LCM default settings response.
/// </summary>
public sealed class ServerLcmDefaultsResponse
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
/// Validation settings response.
/// </summary>
public sealed class ValidationSettingsResponse
{
    public required bool RequireSemVer { get; init; }
    public required ParameterValidationMode DefaultParameterValidationMode { get; init; }
    public required bool AllowConfigurationOverride { get; init; }
    public required bool AllowParameterValidationOverride { get; init; }
}

/// <summary>
/// Request to update validation settings.
/// </summary>
public sealed class UpdateValidationSettingsRequest
{
    public bool? RequireSemVer { get; init; }
    public ParameterValidationMode? DefaultParameterValidationMode { get; init; }
    public bool? AllowConfigurationOverride { get; init; }
    public bool? AllowParameterValidationOverride { get; init; }
}

/// <summary>
/// Global retention policy settings.
/// </summary>
public sealed class RetentionSettingsResponse
{
    public required bool Enabled { get; init; }
    public required int KeepVersions { get; init; }
    public required int KeepDays { get; init; }
    public required bool KeepReleaseVersions { get; init; }
    public required int ScheduleIntervalHours { get; init; }
    public required int ReportKeepCount { get; init; }
    public required int ReportKeepDays { get; init; }
    public required int StatusEventKeepCount { get; init; }
    public required int StatusEventKeepDays { get; init; }
}

/// <summary>
/// Request to update global retention policy settings. Null fields leave existing values unchanged.
/// </summary>
public sealed class UpdateRetentionSettingsRequest
{
    public bool? Enabled { get; init; }
    public int? KeepVersions { get; init; }
    public int? KeepDays { get; init; }
    public bool? KeepReleaseVersions { get; init; }
    public int? ScheduleIntervalHours { get; init; }
    public int? ReportKeepCount { get; init; }
    public int? ReportKeepDays { get; init; }
    public int? StatusEventKeepCount { get; init; }
    public int? StatusEventKeepDays { get; init; }
}

/// <summary>
/// Summary of a retention cleanup run.
/// </summary>
public sealed class RetentionRunSummary
{
    public required Guid Id { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public required string VersionType { get; init; }
    public required bool IsScheduled { get; init; }
    public required bool IsDryRun { get; init; }
    public required int DeletedCount { get; init; }
    public required int KeptCount { get; init; }
    public string? Error { get; init; }
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
