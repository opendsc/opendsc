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
