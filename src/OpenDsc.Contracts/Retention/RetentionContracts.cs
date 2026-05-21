// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Retention;

/// <summary>
/// The type of versioned entity targeted by a retention run.
/// </summary>
public enum RetentionVersionType
{
    /// <summary>Configuration versions.</summary>
    Configuration = 0,

    /// <summary>Parameter file versions.</summary>
    Parameter = 1,

    /// <summary>Composite configuration versions.</summary>
    CompositeConfiguration = 2,

    /// <summary>Compliance report records.</summary>
    Report = 3,

    /// <summary>LCM node status event records.</summary>
    NodeStatusEvent = 4
}

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
public sealed class RetentionRunSummary
{
    /// <summary>
    /// The unique identifier for the retention run.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// When the retention run started.
    /// </summary>
    public DateTimeOffset StartedAt { get; set; }

    /// <summary>
    /// When the retention run completed, or null if still in progress.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// The type of versioned entity targeted by this run.
    /// </summary>
    public RetentionVersionType VersionType { get; set; }

    /// <summary>
    /// Whether this run was triggered by the background scheduler.
    /// </summary>
    public bool IsScheduled { get; set; }

    /// <summary>
    /// Whether this was a dry run (no versions were actually deleted).
    /// </summary>
    public bool IsDryRun { get; set; }

    /// <summary>
    /// Number of versions deleted during this run.
    /// </summary>
    public int DeletedCount { get; set; }

    /// <summary>
    /// Number of versions retained during this run.
    /// </summary>
    public int KeptCount { get; set; }

    /// <summary>
    /// Error message if the run failed, or null on success.
    /// </summary>
    public string? Error { get; set; }
}
