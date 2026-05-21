// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.EntityFrameworkCore;

using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Retention;
using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Services;

public sealed class SettingsService(ServerDbContext db) : ISettingsService
{
    public async Task<ServerSettingsSummary> GetServerSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetServerSettingsEntityAsync(cancellationToken);

        return new ServerSettingsSummary
        {
            CertificateRotationInterval = settings.CertificateRotationInterval,
            StalenessMultiplier = settings.StalenessMultiplier
        };
    }

    public async Task<ServerSettingsSummary> UpdateServerSettingsAsync(
        UpdateServerSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetServerSettingsEntityAsync(cancellationToken);

        if (request.CertificateRotationInterval.HasValue)
        {
            settings.CertificateRotationInterval = request.CertificateRotationInterval.Value;
        }

        if (request.StalenessMultiplier.HasValue)
        {
            settings.StalenessMultiplier = request.StalenessMultiplier.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new ServerSettingsSummary
        {
            CertificateRotationInterval = settings.CertificateRotationInterval,
            StalenessMultiplier = settings.StalenessMultiplier
        };
    }

    public async Task<ServerLcmDefaultsSummary> GetServerLcmDefaultsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetServerSettingsEntityAsync(cancellationToken);

        return new ServerLcmDefaultsSummary
        {
            DefaultConfigurationMode = settings.DefaultConfigurationMode,
            DefaultConfigurationModeInterval = settings.DefaultConfigurationModeInterval,
            DefaultReportCompliance = settings.DefaultReportCompliance
        };
    }

    public async Task<ServerLcmDefaultsSummary> UpdateServerLcmDefaultsAsync(
        UpdateServerLcmDefaultsRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetServerSettingsEntityAsync(cancellationToken);

        settings.DefaultConfigurationMode = request.DefaultConfigurationMode;
        settings.DefaultConfigurationModeInterval = request.DefaultConfigurationModeInterval;
        settings.DefaultReportCompliance = request.DefaultReportCompliance;

        await db.SaveChangesAsync(cancellationToken);

        return new ServerLcmDefaultsSummary
        {
            DefaultConfigurationMode = settings.DefaultConfigurationMode,
            DefaultConfigurationModeInterval = settings.DefaultConfigurationModeInterval,
            DefaultReportCompliance = settings.DefaultReportCompliance
        };
    }

    public async Task<PublicSettingsResponse> GetPublicSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetServerSettingsEntityAsync(cancellationToken);

        return new PublicSettingsResponse
        {
            CertificateRotationInterval = settings.CertificateRotationInterval
        };
    }

    public async Task<ValidationSettingsSummary> GetValidationSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await db.Set<ValidationSettings>().FirstOrDefaultAsync(cancellationToken)
                       ?? new ValidationSettings();

        return ToValidationResponse(settings);
    }

    public async Task<ValidationSettingsSummary> UpdateValidationSettingsAsync(
        UpdateValidationSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = await db.Set<ValidationSettings>().FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new ValidationSettings
            {
                Id = Guid.NewGuid(),
                EnforceSemverCompliance = request.RequireSemVer ?? true,
                DefaultParameterValidation = request.DefaultParameterValidationMode ?? ParameterValidationMode.Strict,
                AllowSemverComplianceOverride = request.AllowConfigurationOverride ?? true,
                AllowParameterValidationOverride = request.AllowParameterValidationOverride ?? true
            };
            db.Add(settings);
        }
        else
        {
            if (request.RequireSemVer.HasValue)
            {
                settings.EnforceSemverCompliance = request.RequireSemVer.Value;
            }

            if (request.DefaultParameterValidationMode.HasValue)
            {
                settings.DefaultParameterValidation = request.DefaultParameterValidationMode.Value;
            }

            if (request.AllowConfigurationOverride.HasValue)
            {
                settings.AllowSemverComplianceOverride = request.AllowConfigurationOverride.Value;
            }

            if (request.AllowParameterValidationOverride.HasValue)
            {
                settings.AllowParameterValidationOverride = request.AllowParameterValidationOverride.Value;
            }
        }

        await db.SaveChangesAsync(cancellationToken);

        return ToValidationResponse(settings);
    }

    public async Task<RetentionSettingsSummary> GetRetentionSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await db.ServerSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return settings is null ? GetDefaultRetentionResponse() : ToRetentionResponse(settings);
    }

    public async Task<RetentionSettingsSummary> UpdateRetentionSettingsAsync(
        UpdateRetentionSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = await db.ServerSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            throw new KeyNotFoundException("Server settings not found.");
        }

        if (request.Enabled.HasValue)
        {
            settings.RetentionEnabled = request.Enabled.Value;
        }

        if (request.KeepVersions.HasValue)
        {
            settings.RetentionKeepVersions = request.KeepVersions.Value;
        }

        if (request.KeepDays.HasValue)
        {
            settings.RetentionKeepDays = request.KeepDays.Value;
        }

        if (request.KeepReleaseVersions.HasValue)
        {
            settings.RetentionKeepReleaseVersions = request.KeepReleaseVersions.Value;
        }

        if (request.ScheduleIntervalHours.HasValue)
        {
            settings.RetentionScheduleInterval = TimeSpan.FromHours(request.ScheduleIntervalHours.Value);
        }

        if (request.ReportKeepCount.HasValue)
        {
            settings.RetentionReportKeepCount = request.ReportKeepCount.Value;
        }

        if (request.ReportKeepDays.HasValue)
        {
            settings.RetentionReportKeepDays = request.ReportKeepDays.Value;
        }

        if (request.StatusEventKeepCount.HasValue)
        {
            settings.RetentionStatusEventKeepCount = request.StatusEventKeepCount.Value;
        }

        if (request.StatusEventKeepDays.HasValue)
        {
            settings.RetentionStatusEventKeepDays = request.StatusEventKeepDays.Value;
        }

        await db.SaveChangesAsync(cancellationToken);

        return ToRetentionResponse(settings);
    }

    public async Task<IReadOnlyList<RetentionRunSummary>> GetRetentionHistoryAsync(CancellationToken cancellationToken = default)
    {
        var runs = await db.RetentionRuns
            .AsNoTracking()
            .OrderByDescending(r => r.StartedAt)
            .ToListAsync(cancellationToken);

        return runs.Select(r => new RetentionRunSummary
        {
            Id = r.Id,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            VersionType = r.VersionType,
            IsScheduled = r.IsScheduled,
            IsDryRun = r.IsDryRun,
            DeletedCount = r.DeletedCount,
            KeptCount = r.KeptCount,
            Error = r.Error
        }).ToList();
    }

    private async Task<ServerSettings> GetServerSettingsEntityAsync(CancellationToken cancellationToken)
    {
        var settings = await db.ServerSettings.FindAsync([1], cancellationToken)
            ?? await db.ServerSettings.FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            throw new KeyNotFoundException("Server settings not found.");
        }

        return settings;
    }

    private static ValidationSettingsSummary ToValidationResponse(ValidationSettings settings)
    {
        return new ValidationSettingsSummary
        {
            RequireSemVer = settings.EnforceSemverCompliance,
            DefaultParameterValidationMode = settings.DefaultParameterValidation,
            AllowConfigurationOverride = settings.AllowSemverComplianceOverride,
            AllowParameterValidationOverride = settings.AllowParameterValidationOverride
        };
    }

    private static RetentionSettingsSummary GetDefaultRetentionResponse()
    {
        return new RetentionSettingsSummary
        {
            Enabled = false,
            KeepVersions = 10,
            KeepDays = 90,
            KeepReleaseVersions = true,
            ScheduleIntervalHours = 24,
            ReportKeepCount = 1000,
            ReportKeepDays = 30,
            StatusEventKeepCount = 200,
            StatusEventKeepDays = 7
        };
    }

    private static RetentionSettingsSummary ToRetentionResponse(ServerSettings settings)
    {
        return new RetentionSettingsSummary
        {
            Enabled = settings.RetentionEnabled,
            KeepVersions = settings.RetentionKeepVersions,
            KeepDays = settings.RetentionKeepDays,
            KeepReleaseVersions = settings.RetentionKeepReleaseVersions,
            ScheduleIntervalHours = (int)settings.RetentionScheduleInterval.TotalHours,
            ReportKeepCount = settings.RetentionReportKeepCount,
            ReportKeepDays = settings.RetentionReportKeepDays,
            StatusEventKeepCount = settings.RetentionStatusEventKeepCount,
            StatusEventKeepDays = settings.RetentionStatusEventKeepDays
        };
    }
}
