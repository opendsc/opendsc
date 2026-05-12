// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Permissions;
using OpenDsc.Contracts.Settings;

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

    private static async Task<Ok<List<ConfigurationSummary>>> GetConfigurations(
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        var result = await configService.GetConfigurationsAsync(cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Created<ConfigurationDetails>, BadRequest<string>, Conflict<string>>> CreateConfiguration(
        HttpRequest httpRequest,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        var form = await httpRequest.ReadFormAsync(cancellationToken);
        var request = BindCreateConfigurationRequest(form);
        var files = form.Files;

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest("Configuration name is required");
        }

        if (files.Count == 0)
        {
            return TypedResults.BadRequest("At least one file is required");
        }

        var entryPoint = string.IsNullOrWhiteSpace(request.EntryPoint) ? "main.dsc.yaml" : request.EntryPoint;

        if (!files.Any(f => f.FileName == entryPoint))
        {
            return TypedResults.BadRequest($"Entry point file '{entryPoint}' not found in uploaded files");
        }

        var version = string.IsNullOrWhiteSpace(request.Version) ? "1.0.0" : request.Version;
        var fileUploads = files.Select(f => new FileUpload(f.FileName, f.OpenReadStream(), f.ContentType, f.Length)).ToList();

        try
        {
            await configService.CreateAsync(
                new CreateConfigurationAdminRequest
                {
                    Name = request.Name,
                    Description = request.Description,
                    EntryPoint = entryPoint,
                    Version = version,
                    UseServerManagedParameters = request.UseServerManagedParameters,
                    Files = fileUploads
                }, cancellationToken);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            return TypedResults.Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }

        var details = await configService.GetConfigurationAsync(request.Name, cancellationToken);
        return TypedResults.Created($"/api/v1/configurations/{request.Name}", details!);
    }

    private static async Task<Results<Ok<ConfigurationDetails>, NotFound, ForbidHttpResult>> GetConfigurationDetails(
        string name,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            var details = await configService.GetConfigurationAsync(name, cancellationToken);
            if (details is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(details);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<Ok<List<ConfigurationVersionDetails>>, NotFound, ForbidHttpResult>> GetConfigurationVersions(
        string name,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            var versions = await configService.GetVersionsAsync(name, cancellationToken);
            if (versions is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(versions);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<Created<ConfigurationVersionDetails>, NotFound, BadRequest<string>, ForbidHttpResult>> CreateConfigurationVersion(
        string name,
        HttpRequest httpRequest,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        var form = await httpRequest.ReadFormAsync(cancellationToken);
        var request = BindCreateConfigurationVersionRequest(form);
        var files = form.Files;

        if (files.Count == 0)
        {
            return TypedResults.BadRequest("At least one file is required");
        }

        var fileUploads = files.Select(f => new FileUpload(f.FileName, f.OpenReadStream(), f.ContentType, f.Length)).ToList();

        try
        {
            await configService.CreateVersionAsync(
                name,
                new CreateConfigurationVersionRequest
                {
                    Version = request.Version,
                    EntryPoint = request.EntryPoint,
                    Files = fileUploads
                },
                cancellationToken);
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

        var versions = await configService.GetVersionsAsync(name, cancellationToken);
        var created = versions?.FirstOrDefault(v => v.Version == request.Version);
        if (created is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Created($"/api/v1/configurations/{name}/versions/{created.Version}", created);
    }

    private static async Task<Results<Ok<ConfigurationVersionDetails>, NotFound, BadRequest<string>, Conflict<CompatibilityReport>, ForbidHttpResult>> PublishConfigurationVersion(
        string name,
        string version,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        PublishResult result;
        try
        {
            result = await configService.PublishVersionAsync(name, version, cancellationToken);
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

        var versions = await configService.GetVersionsAsync(name, cancellationToken);
        var versionDto = versions?.FirstOrDefault(v => v.Version == version);
        if (versionDto is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(versionDto);
    }

    private static async Task<Results<Ok<ConfigurationDetails>, NotFound, Conflict<ErrorResponse>, ForbidHttpResult>> UpdateConfiguration(
        string name,
        [FromBody] UpdateConfigurationAdminRequest request,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            var details = await configService.UpdateAsync(name, request, cancellationToken);
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
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            await configService.DeleteAsync(name, cancellationToken);
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
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            await configService.DeleteVersionAsync(name, version, cancellationToken);
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
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        Stream? stream;
        try
        {
            stream = await configService.DownloadFileAsync(name, version, filePath, cancellationToken);
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

    private static CreateConfigurationAdminRequest BindCreateConfigurationRequest(IFormCollection form)
    {
        return new CreateConfigurationAdminRequest
        {
            Name = GetFormValue(form, "name"),
            Description = GetOptionalFormValue(form, "description"),
            EntryPoint = GetOptionalFormValue(form, "entryPoint") ?? string.Empty,
            Version = GetOptionalFormValue(form, "version") ?? string.Empty,
            UseServerManagedParameters = GetFormBool(form, "useServerManagedParameters")
        };
    }

    private static CreateConfigurationVersionRequest BindCreateConfigurationVersionRequest(IFormCollection form)
    {
        return new CreateConfigurationVersionRequest
        {
            Version = GetFormValue(form, "version"),
            EntryPoint = GetOptionalFormValue(form, "entryPoint")
        };
    }

    private static string GetFormValue(IFormCollection form, string fieldName)
    {
        return GetOptionalFormValue(form, fieldName) ?? string.Empty;
    }

    private static string? GetOptionalFormValue(IFormCollection form, string fieldName)
    {
        var pascalCase = char.ToUpperInvariant(fieldName[0]) + fieldName[1..];
        var candidates = new[]
        {
            fieldName,
            $"request.{fieldName}",
            pascalCase,
            $"request.{pascalCase}"
        };

        foreach (var candidate in candidates)
        {
            if (form.TryGetValue(candidate, out var value) && !StringValues.IsNullOrEmpty(value))
            {
                return value.ToString();
            }
        }

        return null;
    }

    private static bool GetFormBool(IFormCollection form, string fieldName)
    {
        var value = GetOptionalFormValue(form, fieldName);
        return bool.TryParse(value, out var result) && result;
    }

    private static async Task<Results<Ok<List<PermissionEntry>>, NotFound, ForbidHttpResult>> GetConfigurationPermissions(
        string name,
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            var permissions = await configService.GetPermissionsAsync(name, cancellationToken);
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
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            await configService.GrantPermissionAsync(name, request, cancellationToken);
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
        IConfigurationService configService,
        CancellationToken cancellationToken)
    {
        try
        {
            await configService.RevokePermissionAsync(name, new RevokePermissionRequest { PrincipalId = principalId, PrincipalType = principalType }, cancellationToken);
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
