// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Endpoints;

internal static class ConfigurationSettingsEndpoints
{
    public static RouteGroupBuilder MapConfigurationSettingsEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/v1/configurations/{configName}/settings")
            .WithTags("Settings")
            .RequireAuthorization(RetentionPermissions.Manage);

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
        IConfigurationService configService)
    {
        var result = await configService.GetConfigurationSettingsAsync(configName);
        if (result is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<ConfigurationSettingsDto>, NotFound, Conflict<string>>> UpdateConfigurationSettings(
        string configName,
        ConfigurationSettingsUpdateRequest request,
        IConfigurationService configService)
    {
        try
        {
            var result = await configService.UpdateConfigurationSettingsAsync(
                configName, request.RequireSemVer, request.ParameterValidationMode);
            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(ex.Message);
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeleteConfigurationSettings(
        string configName,
        IConfigurationService configService)
    {
        var settings = await configService.GetConfigurationSettingsAsync(configName);
        if (settings is null)
        {
            return TypedResults.NotFound();
        }

        await configService.DeleteConfigurationSettingsAsync(configName);
        return TypedResults.NoContent();
    }
}

public sealed record ConfigurationSettingsUpdateRequest
{
    public bool? RequireSemVer { get; init; }
    public ParameterValidationMode? ParameterValidationMode { get; init; }
}

public sealed record UpdateConfigurationRetentionRequest
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
        IConfigurationService configService)
    {
        var result = await configService.GetRetentionSettingsAsync(configName);
        if (result is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(result);
    }

    public static async Task<Results<Ok<ConfigurationRetentionDto>, NotFound>> UpdateConfigurationRetentionSettings(
        string configName,
        UpdateConfigurationRetentionRequest request,
        IConfigurationService configService)
    {
        var existing = await configService.GetRetentionSettingsAsync(configName);
        if (existing is null)
        {
            return TypedResults.NotFound();
        }

        await configService.SaveRetentionSettingsAsync(configName, request.Enabled, request.KeepVersions, request.KeepDays, request.KeepReleaseVersions);

        var result = await configService.GetRetentionSettingsAsync(configName);
        return TypedResults.Ok(result!);
    }

    public static async Task<Results<NoContent, NotFound>> DeleteConfigurationRetentionSettings(
        string configName,
        IConfigurationService configService)
    {
        var existing = await configService.GetRetentionSettingsAsync(configName);
        if (existing is null)
        {
            return TypedResults.NotFound();
        }

        await configService.ResetRetentionSettingsAsync(configName);
        return TypedResults.NoContent();
    }
}
