// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;

using OpenDsc.Contracts.Configurations;
using OpenDsc.Server.Authorization;

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

    private static async Task<Results<Ok<ConfigurationSettingsSummary>, NotFound>> GetConfigurationSettings(
        string configName,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        var result = await configService.GetSettingsAsync(configName, cancellationToken);
        if (result is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<ConfigurationSettingsSummary>, NotFound, Conflict<string>>> UpdateConfigurationSettings(
        string configName,
        UpdateConfigurationSettingsRequest request,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await configService.UpdateSettingsAsync(configName, request, cancellationToken);
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
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        var settings = await configService.GetSettingsAsync(configName, cancellationToken);
        if (settings is null)
        {
            return TypedResults.NotFound();
        }

        await configService.DeleteSettingsAsync(configName, cancellationToken);
        return TypedResults.NoContent();
    }
}

internal static partial class ConfigurationRetentionHandlers
{
    public static async Task<Results<Ok<ConfigurationRetentionSummary>, NotFound>> GetConfigurationRetentionSettings(
        string configName,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        var result = await configService.GetRetentionSettingsAsync(configName, cancellationToken);
        if (result is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(result);
    }

    public static async Task<Results<Ok<ConfigurationRetentionSummary>, NotFound>> UpdateConfigurationRetentionSettings(
        string configName,
        SaveRetentionSettingsRequest request,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        var existing = await configService.GetRetentionSettingsAsync(configName, cancellationToken);
        if (existing is null)
        {
            return TypedResults.NotFound();
        }

        await configService.SaveRetentionSettingsAsync(configName, request, cancellationToken);

        var result = await configService.GetRetentionSettingsAsync(configName, cancellationToken);
        return TypedResults.Ok(result!);
    }

    public static async Task<Results<NoContent, NotFound>> DeleteConfigurationRetentionSettings(
        string configName,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        var existing = await configService.GetRetentionSettingsAsync(configName, cancellationToken);
        if (existing is null)
        {
            return TypedResults.NotFound();
        }

        await configService.ResetRetentionSettingsAsync(configName, cancellationToken);
        return TypedResults.NoContent();
    }
}
