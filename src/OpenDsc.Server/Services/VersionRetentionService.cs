// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Data;

namespace OpenDsc.Server.Services;

/// <summary>
/// Implements version retention and cleanup policies.
/// </summary>
public sealed partial class VersionRetentionService(
    ServerDbContext db,
    IConfiguration config,
    ILogger<VersionRetentionService> logger) : IVersionRetentionService
{
    public async Task<VersionRetentionResult> CleanupConfigurationVersionsAsync(
        int keepVersions,
        int keepDays,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (keepVersions < 1)
        {
            throw new ArgumentException("Must keep at least 1 version", nameof(keepVersions));
        }

        if (keepDays < 0)
        {
            throw new ArgumentException("Keep days must be non-negative", nameof(keepDays));
        }

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-keepDays);
        var deletedVersions = new List<VersionDeletionInfo>();
        var keptCount = 0;

        var configurations = await db.Configurations
            .Include(c => c.Versions)
            .ToListAsync(cancellationToken);

        foreach (var configuration in configurations)
        {
            var versions = configuration.Versions
                .Where(v => !v.IsDraft)
                .OrderByDescending(v => v.CreatedAt)
                .ToList();

            for (int i = 0; i < versions.Count; i++)
            {
                var version = versions[i];
                var isInActiveUse = await db.NodeConfigurations
                    .AnyAsync(nc => nc.ActiveVersionId == version.Id, cancellationToken);

                if (isInActiveUse)
                {
                    keptCount++;
                    LogVersionInActiveUse(configuration.Name, version.Version);
                    continue;
                }

                var shouldKeep = i < keepVersions || version.CreatedAt >= cutoffDate;

                if (shouldKeep)
                {
                    keptCount++;
                    continue;
                }

                var reason = i >= keepVersions
                    ? $"Exceeds retention count (keeping {keepVersions} versions)"
                    : $"Older than retention period (keeping {keepDays} days)";

                deletedVersions.Add(new VersionDeletionInfo
                {
                    Id = version.Id,
                    Version = version.Version,
                    Name = configuration.Name,
                    CreatedAt = version.CreatedAt,
                    Reason = reason
                });

                if (!dryRun)
                {
                    var dataDir = config["DataDirectory"] ?? "data";
                    var versionDir = Path.Combine(dataDir, "configurations", configuration.Name, $"v{version.Version}");
                    if (Directory.Exists(versionDir))
                    {
                        Directory.Delete(versionDir, true);
                    }

                    db.ConfigurationVersions.Remove(version);
                    LogConfigurationVersionDeleted(configuration.Name, version.Version);
                }
            }
        }

        if (!dryRun && deletedVersions.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        LogCleanupCompleted("configuration", deletedVersions.Count, keptCount, dryRun);

        return new VersionRetentionResult
        {
            DeletedCount = deletedVersions.Count,
            KeptCount = keptCount,
            IsDryRun = dryRun,
            DeletedVersions = deletedVersions
        };
    }

    public async Task<VersionRetentionResult> CleanupParameterVersionsAsync(
        int keepVersions,
        int keepDays,
        bool dryRun = false,
        CancellationToken cancellationToken = default)
    {
        if (keepVersions < 1)
        {
            throw new ArgumentException("Must keep at least 1 version", nameof(keepVersions));
        }

        if (keepDays < 0)
        {
            throw new ArgumentException("Keep days must be non-negative", nameof(keepDays));
        }

        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-keepDays);
        var deletedVersions = new List<VersionDeletionInfo>();
        var keptCount = 0;

        var parameterGroups = await db.ParameterVersions
            .Include(pv => pv.Scope)
            .Include(pv => pv.Configuration)
            .GroupBy(pv => new { pv.ScopeId, pv.ConfigurationId })
            .ToListAsync(cancellationToken);

        foreach (var group in parameterGroups)
        {
            var versions = group
                .Where(v => !v.IsDraft)
                .OrderByDescending(v => v.CreatedAt)
                .ToList();

            var scopeName = versions.FirstOrDefault()?.Scope.Name ?? "Unknown";
            var configName = versions.FirstOrDefault()?.Configuration.Name ?? "Unknown";

            for (int i = 0; i < versions.Count; i++)
            {
                var version = versions[i];

                if (version.IsActive)
                {
                    keptCount++;
                    LogParameterVersionActive(scopeName, configName, version.Version);
                    continue;
                }

                var shouldKeep = i < keepVersions || version.CreatedAt >= cutoffDate;

                if (shouldKeep)
                {
                    keptCount++;
                    continue;
                }

                var reason = i >= keepVersions
                    ? $"Exceeds retention count (keeping {keepVersions} versions)"
                    : $"Older than retention period (keeping {keepDays} days)";

                deletedVersions.Add(new VersionDeletionInfo
                {
                    Id = version.Id,
                    Version = version.Version,
                    Name = $"{scopeName}/{configName}",
                    CreatedAt = version.CreatedAt,
                    Reason = reason
                });

                if (!dryRun)
                {
                    var dataDir = config["DataDirectory"] ?? "data";
                    var versionDir = Path.Combine(dataDir, "parameters", scopeName, configName, $"v{version.Version}");
                    if (Directory.Exists(versionDir))
                    {
                        Directory.Delete(versionDir, true);
                    }

                    db.ParameterVersions.Remove(version);
                    LogParameterVersionDeleted(scopeName, configName, version.Version);
                }
            }
        }

        if (!dryRun && deletedVersions.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        LogCleanupCompleted("parameter", deletedVersions.Count, keptCount, dryRun);

        return new VersionRetentionResult
        {
            DeletedCount = deletedVersions.Count,
            KeptCount = keptCount,
            IsDryRun = dryRun,
            DeletedVersions = deletedVersions
        };
    }

    [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "Configuration version {ConfigurationName} v{Version} is in active use, keeping")]
    private partial void LogVersionInActiveUse(string configurationName, string version);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Information, Message = "Parameter version {ScopeName}/{ConfigurationName} v{Version} is active, keeping")]
    private partial void LogParameterVersionActive(string scopeName, string configurationName, string version);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Information, Message = "Deleted configuration version {ConfigurationName} v{Version}")]
    private partial void LogConfigurationVersionDeleted(string configurationName, string version);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Information, Message = "Deleted parameter version {ScopeName}/{ConfigurationName} v{Version}")]
    private partial void LogParameterVersionDeleted(string scopeName, string configurationName, string version);

    [LoggerMessage(EventId = 5005, Level = LogLevel.Information, Message = "Cleanup completed for {VersionType} versions: {Deleted} deleted, {Kept} kept (dry-run: {DryRun})")]
    private partial void LogCleanupCompleted(string versionType, int deleted, int kept, bool dryRun);
}
