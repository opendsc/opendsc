// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

/// <summary>
/// Defines the retention policy to apply during a cleanup run.
/// </summary>
public sealed record RetentionPolicy
{
    /// <summary>Number of recent non-draft versions to keep per configuration or parameter group.</summary>
    public int KeepVersions { get; init; } = 10;

    /// <summary>Versions older than this many days are candidates for deletion.</summary>
    public int KeepDays { get; init; } = 90;

    /// <summary>When true, release (non-prerelease) versions are never deleted.</summary>
    public bool KeepReleaseVersions { get; init; } = true;

    /// <summary>When true, returns what would be deleted without actually deleting.</summary>
    public bool DryRun { get; init; } = false;

    /// <summary>True when triggered by the background scheduler; false for manual API calls.</summary>
    public bool IsScheduled { get; init; } = false;
}

/// <summary>
/// Retention policy for record-based data such as compliance reports and LCM status events.
/// </summary>
public sealed record RecordRetentionPolicy
{
    /// <summary>Maximum number of records to keep per node.</summary>
    public int KeepCount { get; init; } = 1000;

    /// <summary>Records older than this many days are candidates for deletion.</summary>
    public int KeepDays { get; init; } = 30;

    /// <summary>When true, returns what would be deleted without actually deleting.</summary>
    public bool DryRun { get; init; } = false;

    /// <summary>True when triggered by the background scheduler; false for manual API calls.</summary>
    public bool IsScheduled { get; init; } = false;
}

/// <summary>
/// Service for managing version retention and cleanup.
/// </summary>
public interface IVersionRetentionService
{
    /// <summary>
    /// Cleans up old configuration versions using the supplied policy.
    /// Per-configuration settings overrides are applied on top of the base policy.
    /// </summary>
    Task<VersionRetentionResult> CleanupConfigurationVersionsAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old parameter file versions using the supplied policy.
    /// Per-configuration settings overrides are applied on top of the base policy.
    /// </summary>
    Task<VersionRetentionResult> CleanupParameterVersionsAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old composite configuration versions using the supplied policy.
    /// </summary>
    Task<VersionRetentionResult> CleanupCompositeConfigurationVersionsAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old compliance reports using the supplied policy.
    /// Keeps the most recent records per node.
    /// </summary>
    Task<VersionRetentionResult> CleanupReportsAsync(
        RecordRetentionPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old LCM status events using the supplied policy.
    /// Keeps the most recent events per node.
    /// </summary>
    Task<VersionRetentionResult> CleanupNodeStatusEventsAsync(
        RecordRetentionPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the most recent retention run history records.
    /// </summary>
    Task<IReadOnlyList<RetentionRun>> GetRunHistoryAsync(
        int limit = 100,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a version retention operation.
/// </summary>
public sealed class VersionRetentionResult
{
    /// <summary>
    /// Number of versions deleted (or would be deleted in dry-run mode).
    /// </summary>
    public required int DeletedCount { get; init; }

    /// <summary>
    /// Number of versions kept.
    /// </summary>
    public required int KeptCount { get; init; }

    /// <summary>
    /// Whether this was a dry-run operation.
    /// </summary>
    public required bool IsDryRun { get; init; }

    /// <summary>
    /// Details of versions that were (or would be) deleted.
    /// </summary>
    public List<VersionDeletionInfo>? DeletedVersions { get; init; }
}

/// <summary>
/// Information about a deleted version.
/// </summary>
public sealed class VersionDeletionInfo
{
    /// <summary>
    /// The version identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// The version string.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// For configuration versions: the configuration name.
    /// For parameter versions: the scope name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// When the version was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Reason for deletion.
    /// </summary>
    public required string Reason { get; init; }
}
