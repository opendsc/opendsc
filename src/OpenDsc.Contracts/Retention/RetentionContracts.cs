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
    public int KeepVersions { get; set; }

    /// <summary>Number of days to keep versions.</summary>
    public int KeepDays { get; set; }

    /// <summary>When true, release (non-prerelease) versions are never deleted.</summary>
    public bool KeepReleaseVersions { get; set; }

    /// <summary>If true, returns what would be deleted without actually deleting.</summary>
    public bool DryRun { get; set; }
}

/// <summary>
/// Request to cleanup old records (compliance reports or LCM status events).
/// </summary>
public sealed class RecordCleanupRequest
{
    /// <summary>Maximum number of records to keep per node.</summary>
    public int KeepCount { get; set; }

    /// <summary>Number of days to keep records.</summary>
    public int KeepDays { get; set; }

    /// <summary>If true, returns what would be deleted without actually deleting.</summary>
    public bool DryRun { get; set; }
}

/// <summary>
/// Summary of a single retention cleanup run.
/// </summary>
public sealed class RetentionRun
{
    public Guid Id { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string VersionType { get; set; } = string.Empty;
    public bool IsScheduled { get; set; }
    public bool IsDryRun { get; set; }
    public int DeletedCount { get; set; }
    public int KeptCount { get; set; }
    public string? Error { get; set; }
}
