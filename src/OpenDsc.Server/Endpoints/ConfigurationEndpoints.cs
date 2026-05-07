// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using OpenDsc.Server.Authorization;
using OpenDsc.Contracts.Permissions;
using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Infrastructure;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Endpoints;

public static class ConfigurationEndpoints
{
    public static void MapConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/configurations")
            .RequireAuthorization()
            .WithTags("Configurations");

        group.MapGet("/", GetConfigurations)
            .WithName("GetConfigurations")
            .WithDescription("Get all configurations");

        group.MapPost("/", CreateConfiguration)
            .WithName("CreateConfiguration")
            .WithDescription("Create a new configuration")
            .DisableAntiforgery();

        group.MapGet("/{name}", GetConfigurationDetails)
            .WithName("GetConfigurationDetails")
            .WithDescription("Get configuration details");

        group.MapGet("/{name}/versions", GetConfigurationVersions)
            .WithName("GetConfigurationVersions")
            .WithDescription("Get all versions of a configuration");

        group.MapPost("/{name}/versions", CreateConfigurationVersion)
            .WithName("CreateConfigurationVersion")
            .WithDescription("Create a new version of a configuration")
            .DisableAntiforgery();

        group.MapPut("/{name}/versions/{version}/publish", PublishConfigurationVersion)
            .WithName("PublishConfigurationVersion")
            .WithDescription("Publish a draft configuration version");

        group.MapPatch("/{name}", UpdateConfiguration)
            .WithName("UpdateConfiguration")
            .WithDescription("Update configuration settings such as description and server-managed parameters");

        group.MapDelete("/{name}", DeleteConfiguration)
            .WithName("DeleteConfiguration")
            .WithDescription("Delete a configuration and all its versions");

        group.MapDelete("/{name}/versions/{version}", DeleteConfigurationVersion)
            .WithName("DeleteConfigurationVersion")
            .WithDescription("Delete a specific version (only if draft and not active)");

        group.MapGet("/{name}/versions/{version}/files/{*filePath}", DownloadConfigurationFile)
            .WithName("DownloadConfigurationFile")
            .WithDescription("Download a specific file from a configuration version");

        group.MapGet("/{name}/permissions", GetConfigurationPermissions)
            .WithName("GetConfigurationPermissions")
            .WithDescription("List all permission grants on a configuration");

        group.MapPut("/{name}/permissions", GrantConfigurationPermission)
            .WithName("GrantConfigurationPermission")
            .WithDescription("Grant or update a permission on a configuration");

        group.MapDelete("/{name}/permissions/{principalType}/{principalId:guid}", RevokeConfigurationPermission)
            .WithName("RevokeConfigurationPermission")
            .WithDescription("Revoke a permission on a configuration");
    }

    private static async Task<Ok<List<ConfigurationSummaryDto>>> GetConfigurations(
        IConfigurationService configService)
    {
        var result = await configService.GetConfigurationsAsync();
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Created<ConfigurationDetailsDto>, BadRequest<string>, Conflict<string>>> CreateConfiguration(
        [FromForm] CreateConfigurationDto request,
        IFormFileCollection files,
        IConfigurationService configService)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest("Configuration name is required");
        }

        if (files.Count == 0)
        {
            return TypedResults.BadRequest("At least one file is required");
        }

        var entryPoint = request.EntryPoint ?? "main.dsc.yaml";

        if (!files.Any(f => f.FileName == entryPoint))
        {
            return TypedResults.BadRequest($"Entry point file '{entryPoint}' not found in uploaded files");
        }

        var version = request.Version ?? "1.0.0";
        var adapted = files.Select(f => new FormFileBrowserFileAdapter(f)).ToList();

        try
        {
            await configService.CreateConfigurationAsync(
                request.Name, request.Description, entryPoint, version,
                isDraft: true, request.UseServerManagedParameters, adapted);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return TypedResults.Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }

        var details = await configService.GetConfigurationAsync(request.Name);
        return TypedResults.Created($"/api/v1/configurations/{request.Name}", details!);
    }

    private static async Task<Results<Ok<ConfigurationDetailsDto>, NotFound, ForbidHttpResult>> GetConfigurationDetails(
        string name,
        IConfigurationService configService)
    {
        var details = await configService.GetConfigurationAsync(name);
        if (details is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(details);
    }

    private static async Task<Results<Ok<List<ConfigurationVersionDto>>, NotFound, ForbidHttpResult>> GetConfigurationVersions(
        string name,
        IConfigurationService configService)
    {
        var versions = await configService.GetVersionsAsync(name);
        if (versions is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(versions);
    }

    private static async Task<Results<Created<ConfigurationVersionDto>, NotFound, BadRequest<string>, ForbidHttpResult>> CreateConfigurationVersion(
        string name,
        [FromForm] CreateConfigurationVersionDto request,
        IFormFileCollection files,
        IConfigurationService configService)
    {
        if (files.Count == 0)
        {
            return TypedResults.BadRequest("At least one file is required");
        }

        var adapted = files.Select(f => new FormFileBrowserFileAdapter(f)).ToList();

        try
        {
            await configService.CreateVersionAsync(
                name, request.Version, isDraft: true, adapted, request.EntryPoint);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }

        var versions = await configService.GetVersionsAsync(name);
        var created = versions?.FirstOrDefault(v => v.Version == request.Version);
        if (created is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Created($"/api/v1/configurations/{name}/versions/{created.Version}", created);
    }

    private static async Task<Results<Ok<ConfigurationVersionDto>, NotFound, BadRequest<string>, Conflict<CompatibilityReport>, ForbidHttpResult>> PublishConfigurationVersion(
        string name,
        string version,
        IConfigurationService configService)
    {
        PublishResult result;
        try
        {
            result = await configService.PublishVersionAsync(name, version);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (FileNotFoundException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }

        if (!result.Success && result.CompatibilityReport is not null)
        {
            return TypedResults.Conflict(result.CompatibilityReport);
        }

        var versions = await configService.GetVersionsAsync(name);
        var versionDto = versions?.FirstOrDefault(v => v.Version == version);
        if (versionDto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(versionDto);
    }

    private static async Task<Results<Ok<ConfigurationDetailsDto>, NotFound, Conflict<ErrorResponse>, ForbidHttpResult>> UpdateConfiguration(
        string name,
        [FromBody] UpdateConfigurationDto request,
        IConfigurationService configService)
    {
        try
        {
            var details = await configService.UpdateConfigurationAsync(name, request.Description, request.UseServerManagedParameters);
            return TypedResults.Ok(details);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> DeleteConfiguration(
        string name,
        IConfigurationService configService)
    {
        try
        {
            await configService.DeleteConfigurationAsync(name);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(ex.Message);
        }
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> DeleteConfigurationVersion(
        string name,
        string version,
        IConfigurationService configService)
    {
        try
        {
            await configService.DeleteVersionAsync(name, version);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(ex.Message);
        }
    }

    private static async Task<Results<FileStreamHttpResult, NotFound, ForbidHttpResult>> DownloadConfigurationFile(
        string name,
        string version,
        string filePath,
        IConfigurationService configService)
    {
        Stream? stream;
        try
        {
            stream = await configService.DownloadFileAsync(name, version, filePath);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }

        if (stream is null)
        {
            return TypedResults.NotFound();
        }

        var fileName = Path.GetFileName(filePath);
        var contentType = filePath.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase) || filePath.EndsWith(".yml", StringComparison.OrdinalIgnoreCase)
            ? "application/x-yaml"
            : filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? "application/json" : "application/octet-stream";

        return TypedResults.File(stream, contentType, fileName);
    }

    private static async Task<Results<Ok<List<PermissionEntry>>, NotFound, ForbidHttpResult>> GetConfigurationPermissions(
        string name,
        IConfigurationService configService)
    {
        try
        {
            var permissions = await configService.GetPermissionsAsync(name);
            if (permissions is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(permissions);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, ForbidHttpResult>> GrantConfigurationPermission(
        string name,
        [FromBody] GrantPermissionRequest request,
        IConfigurationService configService)
    {
        try
        {
            await configService.GrantPermissionAsync(name, request.PrincipalId, request.PrincipalType, request.Level);
            return TypedResults.Ok();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<NoContent, BadRequest<string>, NotFound, ForbidHttpResult>> RevokeConfigurationPermission(
        string name,
        string principalType,
        Guid principalId,
        IConfigurationService configService)
    {
        try
        {
            await configService.RevokePermissionAsync(name, principalId, principalType);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }
}
