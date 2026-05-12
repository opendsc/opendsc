// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Parameters;
using OpenDsc.Contracts.Permissions;
using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Authorization;

using ValidationResult = OpenDsc.Contracts.Parameters.ValidationResult;
using ParametersPublishResult = OpenDsc.Contracts.Parameters.PublishResult;

namespace OpenDsc.Server.Endpoints;

public static class ParameterEndpoints
{
    public static IEndpointRouteBuilder MapParameterEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/parameters")
            .WithTags("Parameters")
            .RequireAuthorization();

        group.MapPut("/{scopeTypeId:guid}/{configurationId:guid}", CreateOrUpdateParameter)
            .WithName("CreateOrUpdateParameter")
            .WithDescription("Create or update a parameter file for a scope type and configuration");

        group.MapGet("/{scopeTypeId:guid}/{configurationId:guid}/versions", GetParameterVersions)
            .WithName("GetParameterVersions")
            .WithDescription("Get all parameter file versions for a scope type and configuration");

        group.MapPut("/{scopeTypeId:guid}/{configurationId:guid}/versions/{version}/publish", PublishParameterVersion)
            .WithName("PublishParameterVersion")
            .WithDescription("Publish a specific parameter version");

        group.MapDelete("/{scopeTypeId:guid}/{configurationId:guid}/versions/{version}", DeleteParameterVersion)
            .WithName("DeleteParameterVersion")
            .WithDescription("Delete a parameter version (only if not active)");

        group.MapGet("/{scopeTypeId:guid}/{configurationId:guid}/majors", GetMajorVersions)
            .WithName("GetMajorVersions")
            .WithDescription("Get all major versions with parameter files");

        group.MapGet("/{scopeTypeId:guid}/{configurationId:guid}/majors/{major:int}", GetActiveParameterForMajor)
            .WithName("GetActiveParameterForMajor")
            .WithDescription("Get active parameter file for a specific major version");

        var nodeGroup = app.MapGroup("/api/v1/nodes/{nodeId:guid}/parameters")
            .WithTags("Parameters")
            .RequireAuthorization(NodePermissions.Read);

        nodeGroup.MapGet("/provenance", GetNodeParameterProvenance)
            .WithName("GetNodeParameterProvenance")
            .WithDescription("Get parameter provenance showing which scope provided each value");

        nodeGroup.MapGet("/resolution", GetNodeParameterResolution)
            .WithName("GetNodeParameterResolution")
            .WithDescription("Preview which parameter version each scope would resolve to for a node, without loading file content");

        var configGroup = app.MapGroup("/api/v1/configurations/{configurationName}/parameters")
            .WithTags("Parameters")
            .RequireAuthorization();

        configGroup.MapPut("", UploadParameterSchema)
            .WithName("UploadParameterSchema")
            .WithDescription("Upload a parameter schema for a configuration")
            .DisableAntiforgery();

        configGroup.MapPost("/validate", ValidateParameterFile)
            .WithName("ValidateParameterFile")
            .WithDescription("Validate a parameter file against a schema version");

        configGroup.MapPost("/parameter-files", UploadParameterFile)
            .WithName("UploadParameterFile")
            .WithDescription("Upload a parameter file for a specific scope")
            .DisableAntiforgery();

        configGroup.MapGet("/permissions", GetParameterPermissions)
            .WithName("GetParameterPermissions")
            .WithDescription("List all permission grants on a configuration's parameter schema");

        configGroup.MapPut("/permissions", GrantParameterPermission)
            .WithName("GrantParameterPermission")
            .WithDescription("Grant or update a permission on a configuration's parameter schema");

        configGroup.MapDelete("/permissions/{principalType}/{principalId:guid}", RevokeParameterPermission)
            .WithName("RevokeParameterPermission")
            .WithDescription("Revoke a permission on a configuration's parameter schema");

        return app;
    }

    private static async Task<Results<Ok<ParameterVersionDetails>, BadRequest<string>, NotFound, ForbidHttpResult>> CreateOrUpdateParameter(
        Guid scopeTypeId,
        Guid configurationId,
        [FromBody] CreateParameterRequest request,
        IParameterService parameterService)
    {
        try
        {
            var result = await parameterService.CreateAsync(scopeTypeId, configurationId, request);
            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (InvalidOperationException)
        {
            // Version already exists — try update
            try
            {
                var existing = (await parameterService.GetVersionsAsync(scopeTypeId, configurationId, request.ScopeValue))
                    .FirstOrDefault(v => v.Version == request.Version);

                if (existing is null)
                    return TypedResults.NotFound();

                await parameterService.UpdateAsync(existing.Id, new UpdateParameterRequest { Content = request.Content ?? string.Empty });
                var updated = (await parameterService.GetVersionsAsync(scopeTypeId, configurationId, request.ScopeValue))
                    .First(v => v.Version == request.Version);
                return TypedResults.Ok(updated);
            }
            catch (KeyNotFoundException)
            {
                return TypedResults.NotFound();
            }
            catch (ArgumentException ex)
            {
                return TypedResults.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return TypedResults.BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return TypedResults.Forbid();
            }
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<Ok<IReadOnlyList<ParameterVersionDetails>>, NotFound, ForbidHttpResult>> GetParameterVersions(
        Guid scopeTypeId,
        Guid configurationId,
        [FromQuery] string? scopeValue,
        IParameterService parameterService)
    {
        try
        {
            var versions = await parameterService.GetVersionsAsync(scopeTypeId, configurationId, scopeValue);
            return TypedResults.Ok(versions);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<Ok<ParameterVersionDetails>, NotFound, Conflict<string>, ForbidHttpResult>> PublishParameterVersion(
        Guid scopeTypeId,
        Guid configurationId,
        string version,
        [FromQuery] string? scopeValue,
        IParameterService parameterService)
    {
        try
        {
            await parameterService.PublishAsync(scopeTypeId, configurationId, scopeValue, version);
            var published = (await parameterService.GetVersionsAsync(scopeTypeId, configurationId, scopeValue))
                .FirstOrDefault(v => v.Version == version);
            if (published is null)
                return TypedResults.NotFound();
            return TypedResults.Ok(published);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<NoContent, NotFound, Conflict<string>, ForbidHttpResult>> DeleteParameterVersion(
        Guid scopeTypeId,
        Guid configurationId,
        string version,
        [FromQuery] string? scopeValue,
        IParameterService parameterService)
    {
        try
        {
            await parameterService.DeleteAsync(scopeTypeId, configurationId, scopeValue, version);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<Ok<ParameterProvenanceDetails>, NotFound>> GetNodeParameterProvenance(
        Guid nodeId,
        [FromQuery] Guid? configurationId,
        IParameterService parameterService)
    {
        try
        {
            var result = await parameterService.GetNodeProvenanceAsync(
                nodeId,
                configurationId ?? Guid.Empty);

            if (result is null)
                return TypedResults.NotFound();

            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Ok<ParameterResolutionDetails>, NotFound>> GetNodeParameterResolution(
        Guid nodeId,
        [FromQuery] Guid? configurationId,
        IParameterService parameterService)
    {
        var result = await parameterService.GetNodeResolutionAsync(nodeId, configurationId);
        if (result is null)
            return TypedResults.NotFound();
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<IReadOnlyList<MajorVersionSummary>>, NotFound, ForbidHttpResult>> GetMajorVersions(
        Guid scopeTypeId,
        Guid configurationId,
        [FromQuery] string? scopeValue,
        IParameterService parameterService)
    {
        try
        {
            var result = await parameterService.GetMajorVersionSummariesAsync(scopeTypeId, configurationId, scopeValue);
            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<Ok<ParameterVersionDetails>, NotFound, ForbidHttpResult>> GetActiveParameterForMajor(
        Guid scopeTypeId,
        Guid configurationId,
        int major,
        [FromQuery] string? scopeValue,
        IParameterService parameterService)
    {
        try
        {
            var result = await parameterService.GetActiveParameterForMajorAsync(scopeTypeId, configurationId, major, scopeValue);
            if (result is null)
                return TypedResults.NotFound();
            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<Ok, Conflict<ParametersPublishResult>, BadRequest<string>, NotFound, ForbidHttpResult>> UploadParameterSchema(
        string configurationName,
        [FromForm] string version,
        [FromForm] IFormFile parametersFile,
        IParameterService parameterService,
        IConfigurationService configurationService)
    {
        try
        {
            var configuration = await configurationService.GetConfigurationAsync(configurationName);
            if (configuration is null)
                return TypedResults.NotFound();

            var result = await parameterService.UploadSchemaAsync(
                configuration.Id, version, parametersFile.OpenReadStream());

            if (!result.Success)
                return TypedResults.Conflict(result);

            return TypedResults.Ok();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<Ok<ValidationResult>, BadRequest<string>, NotFound, ForbidHttpResult>> ValidateParameterFile(
        string configurationName,
        [FromQuery] string version,
        [FromBody] string parameterContent,
        IParameterService parameterService,
        IConfigurationService configurationService)
    {
        try
        {
            var configuration = await configurationService.GetConfigurationAsync(configurationName);
            if (configuration is null)
                return TypedResults.NotFound();

            var result = await parameterService.ValidateAsync(configuration.Id, version, parameterContent);
            return TypedResults.Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, ForbidHttpResult>> UploadParameterFile(
        string configurationName,
        [FromForm] string scopeTypeName,
        [FromForm] string version,
        [FromForm] IFormFile parametersFile,
        [FromForm] string? scopeValue,
        IParameterService parameterService,
        IConfigurationService configurationService,
        IScopeService scopeService)
    {
        try
        {
            var configuration = await configurationService.GetConfigurationAsync(configurationName);
            if (configuration is null)
                return TypedResults.NotFound();

            var scopeTypes = await scopeService.GetScopeTypesAsync();
            var scopeType = scopeTypes.FirstOrDefault(st => st.Name == scopeTypeName);
            if (scopeType is null)
                return TypedResults.NotFound();

            using var reader = new StreamReader(parametersFile.OpenReadStream());
            var content = await reader.ReadToEndAsync();

            await parameterService.CreateAsync(scopeType.Id, configuration.Id,
                new CreateParameterRequest { Version = version, ScopeValue = scopeValue, Content = content });

            await parameterService.PublishAsync(scopeType.Id, configuration.Id, scopeValue, version);

            return TypedResults.Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<Ok<List<PermissionEntry>>, NotFound, ForbidHttpResult>> GetParameterPermissions(
        string configurationName,
        IParameterService parameterService,
        IConfigurationService configurationService)
    {
        try
        {
            var configuration = await configurationService.GetConfigurationAsync(configurationName);
            if (configuration is null)
                return TypedResults.NotFound();

            var result = await parameterService.GetPermissionsAsync(configuration.Id);
            if (result is null)
                return TypedResults.NotFound();
            return TypedResults.Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<Ok, BadRequest<string>, NotFound, ForbidHttpResult>> GrantParameterPermission(
        string configurationName,
        [FromBody] GrantPermissionRequest request,
        IParameterService parameterService,
        IConfigurationService configurationService)
    {
        try
        {
            var configuration = await configurationService.GetConfigurationAsync(configurationName);
            if (configuration is null)
                return TypedResults.NotFound();

            await parameterService.GrantPermissionAsync(configuration.Id, request);
            return TypedResults.Ok();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<NoContent, BadRequest<string>, NotFound, ForbidHttpResult>> RevokeParameterPermission(
        string configurationName,
        string principalType,
        Guid principalId,
        IParameterService parameterService,
        IConfigurationService configurationService)
    {
        try
        {
            var configuration = await configurationService.GetConfigurationAsync(configurationName);
            if (configuration is null)
                return TypedResults.NotFound();

            await parameterService.RevokePermissionAsync(configuration.Id, new RevokePermissionRequest
            {
                PrincipalId = principalId,
                PrincipalType = principalType
            });
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }
}
