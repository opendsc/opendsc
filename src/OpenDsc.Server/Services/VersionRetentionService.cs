// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

/// <summary>
/// Implements version retention and cleanup policies.
/// </summary>
public sealed partial class VersionRetentionService(
    ServerDbContext db,
    IOptions<ServerConfig> serverConfig,
    ILogger<VersionRetentionService> logger) : IVersionRetentionService
{
    public async Task<VersionRetentionResult> CleanupConfigurationVersionsAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var deletedVersions = new List<VersionDeletionInfo>();
        var keptCount = 0;

        var configurations = await db.Configurations
            .Include(c => c.Versions)
            .ToListAsync(cancellationToken);

        foreach (var configuration in configurations)
        {
            var effective = await ResolvePolicyForConfigAsync(policy, configuration.Id, cancellationToken);
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-effective.KeepDays);

            var versions = configuration.Versions
                .Where(v => v.Status == ConfigurationVersionStatus.Published)
                .OrderByDescending(v => v.CreatedAt)
                .ToList();

            for (var i = 0; i < versions.Count; i++)
            {
                var version = versions[i];

                var isInActiveUse = await db.NodeConfigurations
                    .AnyAsync(nc => nc.ConfigurationId == configuration.Id
                                    && nc.ActiveVersion == version.Version, cancellationToken);

                var isUsedInComposite = await db.Set<CompositeConfigurationItem>()
                    .AnyAsync(cci => cci.ChildConfigurationId == configuration.Id
                                     && cci.ActiveVersion == version.Version, cancellationToken);

                if (isInActiveUse || isUsedInComposite)
                {
                    keptCount++;
                    LogVersionInActiveUse(configuration.Name, version.Version);
                    continue;
                }

                if (ShouldKeep(i, effective, version.CreatedAt, cutoffDate,
                        version.PrereleaseChannel is null))
                {
                    keptCount++;
                    continue;
                }

                deletedVersions.Add(new VersionDeletionInfo
                {
                    Id = version.Id,
                    Version = version.Version,
                    Name = configuration.Name,
                    CreatedAt = version.CreatedAt,
                    Reason = BuildDeletionReason(i, effective, version.CreatedAt, cutoffDate)
                });

                if (!policy.DryRun)
                {
                    var dataDir = serverConfig.Value.ConfigurationsDirectory;
                    var versionDir = Path.Combine(dataDir, configuration.Name, $"v{version.Version}");
                    if (Directory.Exists(versionDir))
                    {
                        Directory.Delete(versionDir, true);
                    }

                    db.ConfigurationVersions.Remove(version);
                    LogConfigurationVersionDeleted(configuration.Name, version.Version);
                }
            }
        }

        if (!policy.DryRun && deletedVersions.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        LogCleanupCompleted("Configuration", deletedVersions.Count, keptCount, policy.DryRun);
        return await PersistRunAsync("Configuration", deletedVersions, keptCount, startedAt, policy, null, cancellationToken);
    }

    public async Task<VersionRetentionResult> CleanupParameterVersionsAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var deletedVersions = new List<VersionDeletionInfo>();
        var keptCount = 0;

        var schemas = await db.Set<ParameterSchema>()
            .Include(ps => ps.ParameterFiles)
            .Include(ps => ps.Configuration)
            .ToListAsync(cancellationToken);

        foreach (var schema in schemas)
        {
            var effective = await ResolvePolicyForConfigAsync(policy, schema.ConfigurationId, cancellationToken);
            var cutoffDate = DateTimeOffset.UtcNow.AddDays(-effective.KeepDays);

            var groups = schema.ParameterFiles
                .GroupBy(pf => (pf.MajorVersion, pf.ScopeTypeId, pf.ScopeValue));

            foreach (var group in groups)
            {
                // Always protect published versions
                keptCount += group.Count(pf => pf.Status == ParameterVersionStatus.Published);

                var candidates = group
                    .Where(pf => pf.Status == ParameterVersionStatus.Draft)
                    .OrderByDescending(pf => pf.CreatedAt)
                    .ToList();

                for (var i = 0; i < candidates.Count; i++)
                {
                    var file = candidates[i];

                    // ParameterFile has no PrereleaseChannel, so KeepReleaseVersions doesn't apply
                    if (ShouldKeep(i, effective, file.CreatedAt, cutoffDate, isRelease: false))
                    {
                        keptCount++;
                        continue;
                    }

                    var scopeLabel = file.ScopeValue ?? "default";
                    deletedVersions.Add(new VersionDeletionInfo
                    {
                        Id = file.Id,
                        Version = file.Version,
                        Name = $"{schema.Configuration.Name}/{scopeLabel}",
                        CreatedAt = file.CreatedAt,
                        Reason = BuildDeletionReason(i, effective, file.CreatedAt, cutoffDate)
                    });

                    if (!policy.DryRun)
                    {
                        db.ParameterFiles.Remove(file);
                        LogParameterVersionDeleted(schema.Configuration.Name, scopeLabel, file.Version);
                    }
                }
            }
        }

        if (!policy.DryRun && deletedVersions.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        LogCleanupCompleted("Parameter", deletedVersions.Count, keptCount, policy.DryRun);
        return await PersistRunAsync("Parameter", deletedVersions, keptCount, startedAt, policy, null, cancellationToken);
    }

    public async Task<VersionRetentionResult> CleanupCompositeConfigurationVersionsAsync(
        RetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var deletedVersions = new List<VersionDeletionInfo>();
        var keptCount = 0;
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-policy.KeepDays);

        var composites = await db.CompositeConfigurations
            .Include(c => c.Versions)
            .ToListAsync(cancellationToken);

        foreach (var composite in composites)
        {
            var versions = composite.Versions
                .Where(v => v.Status == ConfigurationVersionStatus.Published)
                .OrderByDescending(v => v.CreatedAt)
                .ToList();

            for (var i = 0; i < versions.Count; i++)
            {
                var version = versions[i];

                var isInActiveUse = await db.NodeConfigurations
                    .AnyAsync(nc => nc.CompositeConfigurationId == composite.Id
                                    && nc.ActiveVersion == version.Version, cancellationToken);

                if (isInActiveUse)
                {
                    keptCount++;
                    LogVersionInActiveUse(composite.Name, version.Version);
                    continue;
                }

                if (ShouldKeep(i, policy, version.CreatedAt, cutoffDate,
                        version.PrereleaseChannel is null))
                {
                    keptCount++;
                    continue;
                }

                deletedVersions.Add(new VersionDeletionInfo
                {
                    Id = version.Id,
                    Version = version.Version,
                    Name = composite.Name,
                    CreatedAt = version.CreatedAt,
                    Reason = BuildDeletionReason(i, policy, version.CreatedAt, cutoffDate)
                });

                if (!policy.DryRun)
                {
                    db.CompositeConfigurationVersions.Remove(version);
                    LogConfigurationVersionDeleted(composite.Name, version.Version);
                }
            }
        }

        if (!policy.DryRun && deletedVersions.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }

        LogCleanupCompleted("CompositeConfiguration", deletedVersions.Count, keptCount, policy.DryRun);
        return await PersistRunAsync("CompositeConfiguration", deletedVersions, keptCount, startedAt, policy, null, cancellationToken);
    }

    public async Task<VersionRetentionResult> CleanupReportsAsync(
        RecordRetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var deletedCount = 0;
        var keptCount = 0;
        var cutoff = DateTimeOffset.UtcNow.AddDays(-policy.KeepDays);

        var nodeIds = await db.Nodes.Select(n => n.Id).ToListAsync(cancellationToken);

        foreach (var nodeId in nodeIds)
        {
            var reports = await db.Reports
                .Where(r => r.NodeId == nodeId)
                .Select(r => new { r.Id, r.Timestamp })
                .ToListAsync(cancellationToken);

            var ordered = reports.OrderByDescending(r => r.Timestamp).ToList();
            var toDelete = new List<Guid>();

            for (var i = 0; i < ordered.Count; i++)
            {
                if (i < policy.KeepCount || ordered[i].Timestamp >= cutoff)
                    keptCount++;
                else
                {
                    toDelete.Add(ordered[i].Id);
                    deletedCount++;
                }
            }

            if (!policy.DryRun && toDelete.Count > 0)
            {
                await db.Reports.Where(r => toDelete.Contains(r.Id)).ExecuteDeleteAsync(cancellationToken);
            }
        }

        LogCleanupCompleted("Report", deletedCount, keptCount, policy.DryRun);
        return await PersistRecordRunAsync("Report", deletedCount, keptCount, startedAt, policy, null, cancellationToken);
    }

    public async Task<VersionRetentionResult> CleanupNodeStatusEventsAsync(
        RecordRetentionPolicy policy,
        CancellationToken cancellationToken = default)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var deletedCount = 0;
        var keptCount = 0;
        var cutoff = DateTimeOffset.UtcNow.AddDays(-policy.KeepDays);

        var nodeIds = await db.Nodes.Select(n => n.Id).ToListAsync(cancellationToken);

        foreach (var nodeId in nodeIds)
        {
            var events = await db.NodeStatusEvents
                .Where(e => e.NodeId == nodeId)
                .Select(e => new { e.Id, e.Timestamp })
                .ToListAsync(cancellationToken);

            // NodeStatusEvent.Id is auto-increment, so descending Id order equals newest-first
            var ordered = events.OrderByDescending(e => e.Id).ToList();
            var toDelete = new List<long>();

            for (var i = 0; i < ordered.Count; i++)
            {
                if (i < policy.KeepCount || ordered[i].Timestamp >= cutoff)
                    keptCount++;
                else
                {
                    toDelete.Add(ordered[i].Id);
                    deletedCount++;
                }
            }

            if (!policy.DryRun && toDelete.Count > 0)
            {
                await db.NodeStatusEvents.Where(e => toDelete.Contains(e.Id)).ExecuteDeleteAsync(cancellationToken);
            }
        }

        LogCleanupCompleted("NodeStatusEvent", deletedCount, keptCount, policy.DryRun);
        return await PersistRecordRunAsync("NodeStatusEvent", deletedCount, keptCount, startedAt, policy, null, cancellationToken);
    }

    public async Task<IReadOnlyList<RetentionRun>> GetRunHistoryAsync(
        int limit = 100,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        return await db.RetentionRuns
            .Where(r => from == null || r.StartedAt >= from)
            .Where(r => to == null || r.StartedAt <= to)
            .OrderByDescending(r => r.StartedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    private async Task<RetentionPolicy> ResolvePolicyForConfigAsync(
        RetentionPolicy basePolicy,
        Guid configurationId,
        CancellationToken cancellationToken)
    {
        var overrides = await db.Set<ConfigurationSettings>()
            .FirstOrDefaultAsync(cs => cs.ConfigurationId == configurationId, cancellationToken);

        if (overrides is null)
        {
            return basePolicy;
        }

        return basePolicy with
        {
            KeepVersions = overrides.RetentionKeepVersions ?? basePolicy.KeepVersions,
            KeepDays = overrides.RetentionKeepDays ?? basePolicy.KeepDays,
            KeepReleaseVersions = overrides.RetentionKeepReleaseVersions ?? basePolicy.KeepReleaseVersions
        };
    }

    private static bool ShouldKeep(
        int index,
        RetentionPolicy policy,
        DateTimeOffset createdAt,
        DateTimeOffset cutoffDate,
        bool isRelease)
    {
        return index < policy.KeepVersions
               || createdAt >= cutoffDate
               || (policy.KeepReleaseVersions && isRelease);
    }

    private static string BuildDeletionReason(
        int index,
        RetentionPolicy policy,
        DateTimeOffset createdAt,
        DateTimeOffset cutoffDate)
    {
        var reasons = new List<string>();
        if (index >= policy.KeepVersions)
        {
            reasons.Add($"exceeds version count (keep {policy.KeepVersions})");
        }

        if (createdAt < cutoffDate)
        {
            reasons.Add($"older than {policy.KeepDays} days");
        }

        return reasons.Count > 0 ? string.Join("; ", reasons) : "outside retention window";
    }

    private async Task<VersionRetentionResult> PersistRunAsync(
        string versionType,
        List<VersionDeletionInfo> deleted,
        int kept,
        DateTimeOffset startedAt,
        RetentionPolicy policy,
        string? error,
        CancellationToken cancellationToken)
    {
        db.RetentionRuns.Add(new RetentionRun
        {
            Id = Guid.NewGuid(),
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
            VersionType = versionType,
            IsScheduled = policy.IsScheduled,
            IsDryRun = policy.DryRun,
            DeletedCount = deleted.Count,
            KeptCount = kept,
            Error = error
        });

        await db.SaveChangesAsync(cancellationToken);

        return new VersionRetentionResult
        {
            DeletedCount = deleted.Count,
            KeptCount = kept,
            IsDryRun = policy.DryRun,
            DeletedVersions = deleted
        };
    }

    private async Task<VersionRetentionResult> PersistRecordRunAsync(
        string versionType,
        int deleted,
        int kept,
        DateTimeOffset startedAt,
        RecordRetentionPolicy policy,
        string? error,
        CancellationToken cancellationToken)
    {
        db.RetentionRuns.Add(new RetentionRun
        {
            Id = Guid.NewGuid(),
            StartedAt = startedAt,
            CompletedAt = DateTimeOffset.UtcNow,
            VersionType = versionType,
            IsScheduled = policy.IsScheduled,
            IsDryRun = policy.DryRun,
            DeletedCount = deleted,
            KeptCount = kept,
            Error = error
        });

        await db.SaveChangesAsync(cancellationToken);

        return new VersionRetentionResult
        {
            DeletedCount = deleted,
            KeptCount = kept,
            IsDryRun = policy.DryRun,
            DeletedVersions = null
        };
    }

    [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "Version {ConfigurationName} v{Version} is in active use, keeping")]
    private partial void LogVersionInActiveUse(string configurationName, string version);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Information, Message = "Deleted configuration version {ConfigurationName} v{Version}")]
    private partial void LogConfigurationVersionDeleted(string configurationName, string version);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Information, Message = "Deleted parameter version {ConfigurationName}/{ScopeLabel} v{Version}")]
    private partial void LogParameterVersionDeleted(string configurationName, string scopeLabel, string version);

    [LoggerMessage(EventId = 5005, Level = LogLevel.Information, Message = "Cleanup completed for {VersionType} versions: {Deleted} deleted, {Kept} kept (dry-run: {DryRun})")]
    private partial void LogCleanupCompleted(string versionType, int deleted, int kept, bool dryRun);
}
