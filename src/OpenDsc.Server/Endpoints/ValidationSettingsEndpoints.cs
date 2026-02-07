// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Endpoints;

internal static class ValidationSettingsEndpoints
{
    public static RouteGroupBuilder MapValidationSettingsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/settings/validation")
            .WithTags("Settings")
            .RequireAuthorization(Permissions.ServerSettings_Write);

        group.MapGet("", GetValidationSettings)
            .WithName("GetValidationSettings");

        group.MapPut("", UpdateValidationSettings)
            .WithName("UpdateValidationSettings");

        return group;
    }

    private static async Task<Ok<ValidationSettingsDto>> GetValidationSettings(ServerDbContext db)
    {
        var settings = await db.Set<ValidationSettings>().FirstOrDefaultAsync()
                       ?? new ValidationSettings();

        return TypedResults.Ok(new ValidationSettingsDto
        {
            RequireSemVer = settings.EnforceSemverCompliance,
            DefaultParameterValidationMode = settings.DefaultParameterValidation,
            AllowConfigurationOverride = settings.AllowSemverComplianceOverride,
            AllowParameterValidationOverride = settings.AllowParameterValidationOverride
        });
    }

    private static async Task<Ok<ValidationSettingsDto>> UpdateValidationSettings(
        ValidationSettingsUpdateRequest request,
        ServerDbContext db)
    {
        var settings = await db.Set<ValidationSettings>().FirstOrDefaultAsync();

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

        await db.SaveChangesAsync();

        return TypedResults.Ok(new ValidationSettingsDto
        {
            RequireSemVer = settings.EnforceSemverCompliance,
            DefaultParameterValidationMode = settings.DefaultParameterValidation,
            AllowConfigurationOverride = settings.AllowSemverComplianceOverride,
            AllowParameterValidationOverride = settings.AllowParameterValidationOverride
        });
    }
}

internal sealed record ValidationSettingsDto
{
    public required bool RequireSemVer { get; init; }
    public required ParameterValidationMode DefaultParameterValidationMode { get; init; }
    public required bool AllowConfigurationOverride { get; init; }
    public required bool AllowParameterValidationOverride { get; init; }
}

internal sealed record ValidationSettingsUpdateRequest
{
    public bool? RequireSemVer { get; init; }
    public ParameterValidationMode? DefaultParameterValidationMode { get; init; }
    public bool? AllowConfigurationOverride { get; init; }
    public bool? AllowParameterValidationOverride { get; init; }
}
