// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Data;

namespace OpenDsc.Server.Services;

/// <summary>
/// Background service that periodically purges versions outside the configured retention policy.
/// Only runs when <see cref="Entities.ServerSettings.RetentionEnabled"/> is true.
/// </summary>
public sealed partial class RetentionBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<RetentionBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarting();

        while (!stoppingToken.IsCancellationRequested)
        {
            TimeSpan interval;
            bool enabled;

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ServerDbContext>();
                var settings = await db.ServerSettings.FirstOrDefaultAsync(stoppingToken);
                interval = settings?.RetentionScheduleInterval ?? TimeSpan.FromHours(24);
                enabled = settings?.RetentionEnabled ?? false;
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            if (!enabled)
            {
                continue;
            }

            LogRunning();

            try
            {
                using var cleanupScope = scopeFactory.CreateScope();
                var db = cleanupScope.ServiceProvider.GetRequiredService<ServerDbContext>();
                var retentionService = cleanupScope.ServiceProvider.GetRequiredService<IVersionRetentionService>();

                var settings = await db.ServerSettings.FirstOrDefaultAsync(stoppingToken);
                if (settings is null || !settings.RetentionEnabled)
                {
                    continue;
                }

                var policy = new RetentionPolicy
                {
                    KeepVersions = settings.RetentionKeepVersions,
                    KeepDays = settings.RetentionKeepDays,
                    KeepReleaseVersions = settings.RetentionKeepReleaseVersions,
                    DryRun = false,
                    IsScheduled = true
                };

                await retentionService.CleanupConfigurationVersionsAsync(policy, stoppingToken);
                await retentionService.CleanupParameterVersionsAsync(policy, stoppingToken);
                await retentionService.CleanupCompositeConfigurationVersionsAsync(policy, stoppingToken);

                var reportPolicy = new RecordRetentionPolicy
                {
                    KeepCount = settings.RetentionReportKeepCount,
                    KeepDays = settings.RetentionReportKeepDays,
                    DryRun = false,
                    IsScheduled = true
                };

                await retentionService.CleanupReportsAsync(reportPolicy, stoppingToken);

                var statusPolicy = new RecordRetentionPolicy
                {
                    KeepCount = settings.RetentionStatusEventKeepCount,
                    KeepDays = settings.RetentionStatusEventKeepDays,
                    DryRun = false,
                    IsScheduled = true
                };

                await retentionService.CleanupNodeStatusEventsAsync(statusPolicy, stoppingToken);

                LogCompleted();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogError(ex);
            }
        }

        LogStopping();
    }

    [LoggerMessage(EventId = 6001, Level = LogLevel.Information, Message = "Retention background service starting")]
    private partial void LogStarting();

    [LoggerMessage(EventId = 6002, Level = LogLevel.Information, Message = "Running scheduled retention cleanup")]
    private partial void LogRunning();

    [LoggerMessage(EventId = 6003, Level = LogLevel.Information, Message = "Scheduled retention cleanup completed")]
    private partial void LogCompleted();

    [LoggerMessage(EventId = 6004, Level = LogLevel.Error, Message = "Scheduled retention cleanup failed")]
    private partial void LogError(Exception ex);

    [LoggerMessage(EventId = 6005, Level = LogLevel.Information, Message = "Retention background service stopping")]
    private partial void LogStopping();
}
