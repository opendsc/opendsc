// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using OpenDsc.Contracts.CompositeConfigurations;
using OpenDsc.Contracts.Permissions;
using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Services;
using ICompositeConfigurationService = OpenDsc.Server.Services.ICompositeConfigurationService;

namespace OpenDsc.Server.Endpoints;

public static class CompositeConfigurationEndpoints
{
    public static void MapCompositeConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/composite-configurations")
            .RequireAuthorization()
            .WithTags("Composite Configurations");

        group.MapGet("/", GetCompositeConfigurations)
            .WithName("GetCompositeConfigurations")
            .WithDescription("Get all composite configurations");

        group.MapPost("/", CreateCompositeConfiguration)
            .WithName("CreateCompositeConfiguration")
            .WithDescription("Create a new composite configuration");

        group.MapGet("/{name}", GetCompositeConfigurationDetails)
            .WithName("GetCompositeConfigurationDetails")
            .WithDescription("Get composite configuration details");

        group.MapDelete("/{name}", DeleteCompositeConfiguration)
            .WithName("DeleteCompositeConfiguration")
            .WithDescription("Delete a composite configuration and all its versions");

        group.MapPost("/{name}/versions", CreateCompositeConfigurationVersion)
            .WithName("CreateCompositeConfigurationVersion")
            .WithDescription("Create a new version of a composite configuration (draft)");

        group.MapGet("/{name}/versions", GetCompositeConfigurationVersions)
            .WithName("GetCompositeConfigurationVersions")
            .WithDescription("Get all versions of a composite configuration");

        group.MapGet("/{name}/versions/{version}", GetCompositeConfigurationVersionDetails)
            .WithName("GetCompositeConfigurationVersionDetails")
            .WithDescription("Get details of a specific composite configuration version");

        group.MapPut("/{name}/versions/{version}/publish", PublishCompositeConfigurationVersion)
            .WithName("PublishCompositeConfigurationVersion")
            .WithDescription("Publish a draft composite configuration version");

        group.MapDelete("/{name}/versions/{version}", DeleteCompositeConfigurationVersion)
            .WithName("DeleteCompositeConfigurationVersion")
            .WithDescription("Delete a specific version (only if draft and not active)");

        group.MapPost("/{name}/versions/{version}/children", AddChildConfiguration)
            .WithName("AddChildConfiguration")
            .WithDescription("Add a child configuration to a draft composite version");

        group.MapPut("/{name}/versions/{version}/children/{childId}", UpdateChildConfiguration)
            .WithName("UpdateChildConfiguration")
            .WithDescription("Update a child configuration in a draft composite version");

        group.MapDelete("/{name}/versions/{version}/children/{childId}", RemoveChildConfiguration)
            .WithName("RemoveChildConfiguration")
            .WithDescription("Remove a child configuration from a draft composite version");

        group.MapGet("/{name}/permissions", GetCompositeConfigurationPermissions)
            .WithName("GetCompositeConfigurationPermissions")
            .WithDescription("List all permission grants on a composite configuration");

        group.MapPut("/{name}/permissions", GrantCompositeConfigurationPermission)
            .WithName("GrantCompositeConfigurationPermission")
            .WithDescription("Grant or update a permission on a composite configuration");

        group.MapDelete("/{name}/permissions/{principalType}/{principalId:guid}", RevokeCompositeConfigurationPermission)
            .WithName("RevokeCompositeConfigurationPermission")
            .WithDescription("Revoke a permission on a composite configuration");
    }

    private static async Task<Ok<List<CompositeConfigurationSummary>>> GetCompositeConfigurations(
        ICompositeConfigurationService compositeService)
    {
        var result = await compositeService.GetCompositeConfigurationsAsync();
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Created<CompositeConfigurationDetails>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> CreateCompositeConfiguration(
        CreateCompositeConfigurationRequest request,
        ICompositeConfigurationService compositeService)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Composite configuration name is required" });
        }

        try
        {
            var details = await compositeService.CreateCompositeConfigurationAsync(request.Name, request.Description);
            return TypedResults.Created($"/api/v1/composite-configurations/{details.Name}", details);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<Ok<CompositeConfigurationDetails>, NotFound, ForbidHttpResult>> GetCompositeConfigurationDetails(
        string name,
        ICompositeConfigurationService compositeService)
    {
        try
        {
            var details = await compositeService.GetCompositeConfigurationAsync(name);
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

    private static async Task<Results<NoContent, NotFound, BadRequest<ErrorResponse>, ForbidHttpResult>> DeleteCompositeConfiguration(
        string name,
        ICompositeConfigurationService compositeService)
    {
        try
        {
            await compositeService.DeleteCompositeConfigurationByNameAsync(name);
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
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<Created<CompositeConfigurationVersionDetails>, NotFound, BadRequest<ErrorResponse>, Conflict<ErrorResponse>, ForbidHttpResult>> CreateCompositeConfigurationVersion(
        string name,
        CreateCompositeConfigurationVersionRequest request,
        ICompositeConfigurationService compositeService)
    {
        if (string.IsNullOrWhiteSpace(request.Version))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Version is required" });
        }

        try
        {
            var version = await compositeService.CreateVersionAsync(name, request.Version);
            return TypedResults.Created($"/api/v1/composite-configurations/{name}/versions/{version.Version}", version);
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

    private static async Task<Results<Ok<List<CompositeConfigurationVersionDetails>>, NotFound, ForbidHttpResult>> GetCompositeConfigurationVersions(
        string name,
        ICompositeConfigurationService compositeService)
    {
        try
        {
            var versions = await compositeService.GetVersionsAsync(name);
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

    private static async Task<Results<Ok<CompositeConfigurationVersionDetails>, NotFound, ForbidHttpResult>> GetCompositeConfigurationVersionDetails(
        string name,
        string version,
        ICompositeConfigurationService compositeService)
    {
        try
        {
            var dto = await compositeService.GetVersionAsync(name, version);
            if (dto is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(dto);
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Forbid();
        }
    }

    private static async Task<Results<Ok, NotFound, BadRequest<ErrorResponse>, ForbidHttpResult>> PublishCompositeConfigurationVersion(
        string name,
        string version,
        ICompositeConfigurationService compositeService)
    {
        try
        {
            await compositeService.PublishVersionAsync(name, version);
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
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ErrorResponse>, ForbidHttpResult>> DeleteCompositeConfigurationVersion(
        string name,
        string version,
        ICompositeConfigurationService compositeService)
    {
        try
        {
            await compositeService.DeleteVersionAsync(name, version);
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
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<Created<CompositeConfigurationItemDetails>, NotFound, BadRequest<ErrorResponse>, Conflict<ErrorResponse>, ForbidHttpResult>> AddChildConfiguration(
        string name,
        string version,
        AddChildConfigurationRequest request,
        ICompositeConfigurationService compositeService)
    {
        try
        {
            var item = await compositeService.AddChildByNameAsync(name, version, request.ChildConfigurationName, request.MajorVersion, request.Order);
            return TypedResults.Created($"/api/v1/composite-configurations/{name}/versions/{version}/children/{item.Id}", item);
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
            if (ex.Message.Contains("already in"))
            {
                return TypedResults.Conflict(new ErrorResponse { Error = ex.Message });
            }

            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<Ok<CompositeConfigurationItemDetails>, NotFound, BadRequest<ErrorResponse>, ForbidHttpResult>> UpdateChildConfiguration(
        string name,
        string version,
        Guid childId,
        UpdateChildConfigurationRequest request,
        ICompositeConfigurationService compositeService)
    {
        try
        {
            var item = await compositeService.UpdateChildAsync(childId, null, request.Order);
            return TypedResults.Ok(item);
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
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<ErrorResponse>, ForbidHttpResult>> RemoveChildConfiguration(
        string name,
        string version,
        Guid childId,
        ICompositeConfigurationService compositeService)
    {
        try
        {
            await compositeService.RemoveChildAsync(childId);
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
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<Ok<List<PermissionEntry>>, NotFound, ForbidHttpResult>> GetCompositeConfigurationPermissions(
        string name,
        ICompositeConfigurationService compositeService)
    {
        try
        {
            var permissions = await compositeService.GetPermissionsAsync(name);
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

    private static async Task<Results<Ok, BadRequest<string>, NotFound, ForbidHttpResult>> GrantCompositeConfigurationPermission(
        string name,
        [FromBody] GrantPermissionRequest request,
        ICompositeConfigurationService compositeService)
    {
        try
        {
            await compositeService.GrantPermissionAsync(name, request.PrincipalId, request.PrincipalType, request.Level);
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

    private static async Task<Results<NoContent, BadRequest<string>, NotFound, ForbidHttpResult>> RevokeCompositeConfigurationPermission(
        string name,
        string principalType,
        Guid principalId,
        ICompositeConfigurationService compositeService)
    {
        try
        {
            await compositeService.RevokePermissionAsync(name, principalId, principalType);
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

