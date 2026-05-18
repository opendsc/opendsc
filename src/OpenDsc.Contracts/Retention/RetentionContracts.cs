// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Retention;

/// <summary>
/// Request to cleanup old versions.
/// </summary>
public sealed class CleanupRequest
{
    /// <summary>Number of recent versions to keep.</summary>
    public required int KeepVersions { get; init; }

    /// <summary>Number of days to keep versions.</summary>
    public required int KeepDays { get; init; }

    /// <summary>When true, release (non-prerelease) versions are never deleted.</summary>
    public required bool KeepReleaseVersions { get; init; }

    /// <summary>If true, returns what would be deleted without actually deleting.</summary>
    public required bool DryRun { get; init; }
}

/// <summary>
/// Request to cleanup old records (compliance reports or LCM status events).
/// </summary>
public sealed class RecordCleanupRequest
{
    /// <summary>Maximum number of records to keep per node.</summary>
    public required int KeepCount { get; init; }

    /// <summary>Number of days to keep records.</summary>
    public required int KeepDays { get; init; }

    /// <summary>If true, returns what would be deleted without actually deleting.</summary>
    public required bool DryRun { get; init; }
}

/// <summary>
/// Summary of a single retention cleanup run.
/// </summary>
public sealed class RetentionRun
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
