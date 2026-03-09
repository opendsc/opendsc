// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Endpoints;

internal static class ConfigurationSettingsEndpoints
{
    public static RouteGroupBuilder MapConfigurationSettingsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/configurations/{configName}/settings")
            .WithTags("Settings")
            .RequireAuthorization(Permissions.Retention_Manage);

        group.MapGet("", GetConfigurationSettings)
            .WithName("GetConfigurationSettings");

        group.MapPut("", UpdateConfigurationSettings)
            .WithName("UpdateConfigurationSettings");

        group.MapDelete("", DeleteConfigurationSettings)
            .WithName("DeleteConfigurationSettings");

        var retentionGroup = group.MapGroup("/retention");

        retentionGroup.MapGet("", ConfigurationRetentionHandlers.GetConfigurationRetentionSettings)
            .WithName("GetConfigurationRetentionSettings")
            .WithSummary("Get per-configuration retention policy overrides");

        retentionGroup.MapPut("", ConfigurationRetentionHandlers.UpdateConfigurationRetentionSettings)
            .WithName("UpdateConfigurationRetentionSettings")
            .WithSummary("Set per-configuration retention policy overrides");

        retentionGroup.MapDelete("", ConfigurationRetentionHandlers.DeleteConfigurationRetentionSettings)
            .WithName("DeleteConfigurationRetentionSettings")
            .WithSummary("Remove per-configuration retention overrides (revert to global)");

        return group;
    }

    private static async Task<Results<Ok<ConfigurationSettingsDto>, NotFound>> GetConfigurationSettings(
        string configName,
        ServerDbContext db)
    {
        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var settings = await db.Set<ConfigurationSettings>()
            .FirstOrDefaultAsync(cs => cs.ConfigurationId == configuration.Id);

        if (settings is null)
        {
            var globalSettings = await db.Set<ValidationSettings>().FirstOrDefaultAsync()
                                 ?? new ValidationSettings();

            return TypedResults.Ok(new ConfigurationSettingsDto
            {
                IsOverridden = false,
                RequireSemVer = globalSettings.EnforceSemverCompliance,
                ParameterValidationMode = globalSettings.DefaultParameterValidation
            });
        }

        return TypedResults.Ok(new ConfigurationSettingsDto
        {
            IsOverridden = true,
            RequireSemVer = settings.EnforceSemverCompliance ?? true,
            ParameterValidationMode = settings.ParameterValidation ?? ParameterValidationMode.Strict
        });
    }

    private static async Task<Results<Ok<ConfigurationSettingsDto>, NotFound, Conflict<string>>> UpdateConfigurationSettings(
        string configName,
        ConfigurationSettingsUpdateRequest request,
        ServerDbContext db)
    {
        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var globalSettings = await db.Set<ValidationSettings>().FirstOrDefaultAsync()
                             ?? new ValidationSettings();

        if (!globalSettings.AllowSemverComplianceOverride || !globalSettings.AllowParameterValidationOverride)
        {
            return TypedResults.Conflict("Configuration-level overrides are not allowed by global settings");
        }

        var settings = await db.Set<ConfigurationSettings>()
            .FirstOrDefaultAsync(cs => cs.ConfigurationId == configuration.Id);

        if (settings is null)
        {
            settings = new ConfigurationSettings
            {
                ConfigurationId = configuration.Id,
                EnforceSemverCompliance = request.RequireSemVer ?? globalSettings.EnforceSemverCompliance,
                ParameterValidation = request.ParameterValidationMode ?? globalSettings.DefaultParameterValidation,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Add(settings);
        }
        else
        {
            if (request.RequireSemVer.HasValue)
            {
                settings.EnforceSemverCompliance = request.RequireSemVer.Value;
            }

            if (request.ParameterValidationMode.HasValue)
            {
                settings.ParameterValidation = request.ParameterValidationMode.Value;
            }

            settings.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();

        return TypedResults.Ok(new ConfigurationSettingsDto
        {
            IsOverridden = true,
            RequireSemVer = settings.EnforceSemverCompliance ?? true,
            ParameterValidationMode = settings.ParameterValidation ?? ParameterValidationMode.Strict
        });
    }

    private static async Task<Results<NoContent, NotFound>> DeleteConfigurationSettings(
        string configName,
        ServerDbContext db)
    {
        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var settings = await db.Set<ConfigurationSettings>()
            .FirstOrDefaultAsync(cs => cs.ConfigurationId == configuration.Id);

        if (settings is not null)
        {
            db.Remove(settings);
            await db.SaveChangesAsync();
        }

        return TypedResults.NoContent();
    }
}

internal sealed record ConfigurationSettingsDto
{
    public required bool IsOverridden { get; init; }
    public required bool RequireSemVer { get; init; }
    public required ParameterValidationMode ParameterValidationMode { get; init; }
}

internal sealed record ConfigurationSettingsUpdateRequest
{
    public bool? RequireSemVer { get; init; }
    public ParameterValidationMode? ParameterValidationMode { get; init; }
}

internal sealed record ConfigurationRetentionDto
{
    public bool IsOverridden { get; init; }
    public bool? Enabled { get; init; }
    public int? KeepVersions { get; init; }
    public int? KeepDays { get; init; }
    public bool? KeepReleaseVersions { get; init; }
}

internal sealed record UpdateConfigurationRetentionRequest
{
    public bool? Enabled { get; init; }
    public int? KeepVersions { get; init; }
    public int? KeepDays { get; init; }
    public bool? KeepReleaseVersions { get; init; }
}

internal static partial class ConfigurationRetentionHandlers
{
    public static async Task<Results<Ok<ConfigurationRetentionDto>, NotFound>> GetConfigurationRetentionSettings(
        string configName,
        ServerDbContext db)
    {
        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var settings = await db.Set<ConfigurationSettings>()
            .FirstOrDefaultAsync(cs => cs.ConfigurationId == configuration.Id);

        if (settings is null || settings.RetentionEnabled is null && settings.RetentionKeepVersions is null
            && settings.RetentionKeepDays is null && settings.RetentionKeepReleaseVersions is null)
        {
            return TypedResults.Ok(new ConfigurationRetentionDto { IsOverridden = false });
        }

        return TypedResults.Ok(new ConfigurationRetentionDto
        {
            IsOverridden = true,
            Enabled = settings.RetentionEnabled,
            KeepVersions = settings.RetentionKeepVersions,
            KeepDays = settings.RetentionKeepDays,
            KeepReleaseVersions = settings.RetentionKeepReleaseVersions
        });
    }

    public static async Task<Results<Ok<ConfigurationRetentionDto>, NotFound>> UpdateConfigurationRetentionSettings(
        string configName,
        UpdateConfigurationRetentionRequest request,
        ServerDbContext db)
    {
        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var settings = await db.Set<ConfigurationSettings>()
            .FirstOrDefaultAsync(cs => cs.ConfigurationId == configuration.Id);

        if (settings is null)
        {
            settings = new ConfigurationSettings
            {
                ConfigurationId = configuration.Id,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.Add(settings);
        }
        else
        {
            settings.UpdatedAt = DateTimeOffset.UtcNow;
        }

        if (request.Enabled.HasValue) { settings.RetentionEnabled = request.Enabled.Value; }
        if (request.KeepVersions.HasValue) { settings.RetentionKeepVersions = request.KeepVersions.Value; }
        if (request.KeepDays.HasValue) { settings.RetentionKeepDays = request.KeepDays.Value; }
        if (request.KeepReleaseVersions.HasValue) { settings.RetentionKeepReleaseVersions = request.KeepReleaseVersions.Value; }

        await db.SaveChangesAsync();

        return TypedResults.Ok(new ConfigurationRetentionDto
        {
            IsOverridden = true,
            Enabled = settings.RetentionEnabled,
            KeepVersions = settings.RetentionKeepVersions,
            KeepDays = settings.RetentionKeepDays,
            KeepReleaseVersions = settings.RetentionKeepReleaseVersions
        });
    }

    public static async Task<Results<NoContent, NotFound>> DeleteConfigurationRetentionSettings(
        string configName,
        ServerDbContext db)
    {
        var configuration = await db.Configurations.FirstOrDefaultAsync(c => c.Name == configName);
        if (configuration is null)
        {
            return TypedResults.NotFound();
        }

        var settings = await db.Set<ConfigurationSettings>()
            .FirstOrDefaultAsync(cs => cs.ConfigurationId == configuration.Id);

        if (settings is not null)
        {
            settings.RetentionEnabled = null;
            settings.RetentionKeepVersions = null;
            settings.RetentionKeepDays = null;
            settings.RetentionKeepReleaseVersions = null;
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        return TypedResults.NoContent();
    }
}
