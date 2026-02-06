// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Services;

/// <summary>
/// Service for managing version retention and cleanup.
/// </summary>
public interface IVersionRetentionService
{
    /// <summary>
    /// Cleans up old configuration versions based on retention policy.
    /// </summary>
    /// <param name="keepVersions">Number of recent versions to keep.</param>
    /// <param name="keepDays">Number of days to keep versions.</param>
    /// <param name="dryRun">If true, returns what would be deleted without actually deleting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Retention result with deleted version information.</returns>
    Task<VersionRetentionResult> CleanupConfigurationVersionsAsync(
        int keepVersions,
        int keepDays,
        bool dryRun = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old parameter versions based on retention policy.
    /// </summary>
    /// <param name="keepVersions">Number of recent versions to keep.</param>
    /// <param name="keepDays">Number of days to keep versions.</param>
    /// <param name="dryRun">If true, returns what would be deleted without actually deleting.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Retention result with deleted version information.</returns>
    Task<VersionRetentionResult> CleanupParameterVersionsAsync(
        int keepVersions,
        int keepDays,
        bool dryRun = false,
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
