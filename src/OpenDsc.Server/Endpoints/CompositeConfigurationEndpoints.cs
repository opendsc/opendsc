// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using OpenDsc.Contracts.CompositeConfigurations;
using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Permissions;
using OpenDsc.Contracts.Settings;

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

        group.MapPost("/{name}/versions/from-existing", CreateCompositeConfigurationVersionFromExisting)
            .WithName("CreateCompositeConfigurationVersionFromExisting")
            .WithDescription("Create a new composite configuration version by copying an existing version");

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

        group.MapGet("/children/available", GetAvailableChildConfigurations)
            .WithName("GetAvailableChildConfigurations")
            .WithDescription("Get available child configurations that can be added to a composite");

        group.MapGet("/children/{configurationId:guid}/major-versions", GetAvailableMajorVersions)
            .WithName("GetAvailableMajorVersions")
            .WithDescription("Get available published major versions for a child configuration");

        group.MapPut("/{name}/versions/{version}/children/{childId}", UpdateChildConfiguration)
            .WithName("UpdateChildConfiguration")
            .WithDescription("Update a child configuration in a draft composite version");

        group.MapDelete("/{name}/versions/{version}/children/{childId}", RemoveChildConfiguration)
            .WithName("RemoveChildConfiguration")
            .WithDescription("Remove a child configuration from a draft composite version");

        group.MapPut("/children/{itemId:guid}", UpdateChildConfigurationByItemId)
            .WithName("UpdateChildConfigurationByItemId")
            .WithDescription("Update a child configuration item by item identifier");

        group.MapDelete("/children/{itemId:guid}", RemoveChildConfigurationByItemId)
            .WithName("RemoveChildConfigurationByItemId")
            .WithDescription("Remove a child configuration item by item identifier");

        group.MapPut("/children/{itemId:guid}/order/{newOrder:int}", ReorderChildConfigurationByItemId)
            .WithName("ReorderChildConfigurationByItemId")
            .WithDescription("Reorder a child configuration item by item identifier");

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

    private static async Task<Ok<IReadOnlyList<CompositeConfigurationSummary>>> GetCompositeConfigurations(
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        var result = await compositeService.GetCompositeConfigurationsAsync(cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Created<CompositeConfigurationDetails>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> CreateCompositeConfiguration(
        CreateCompositeConfigurationRequest request,
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Composite configuration name is required" });
        }

        try
        {
            var details = await compositeService.CreateAsync(request, cancellationToken);
            return TypedResults.Created($"/api/v1/composite-configurations/{details.Name}", details);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<Ok<CompositeConfigurationDetails>, NotFound, ForbidHttpResult>> GetCompositeConfigurationDetails(
        string name,
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var details = await compositeService.GetCompositeConfigurationAsync(name, cancellationToken);
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
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await compositeService.DeleteAsync(name, cancellationToken);
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
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Version))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Version is required" });
        }

        try
        {
            var version = await compositeService.CreateVersionAsync(name, request, cancellationToken);
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

    private static async Task<Results<NoContent, NotFound, BadRequest<ErrorResponse>, Conflict<ErrorResponse>, ForbidHttpResult>> CreateCompositeConfigurationVersionFromExisting(
        string name,
        CreateCompositeVersionFromExistingRequest request,
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await compositeService.CreateVersionFromExistingAsync(name, request, cancellationToken);
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
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.Conflict(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Ok<IReadOnlyList<ChildConfigurationOption>>> GetAvailableChildConfigurations(
        [FromQuery] Guid[]? excludeIds,
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        var result = await compositeService.GetAvailableChildConfigurationsAsync(excludeIds ?? [], cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Ok<IReadOnlyList<int>>> GetAvailableMajorVersions(
        Guid configurationId,
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        var result = await compositeService.GetAvailableMajorVersionsAsync(configurationId, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<IReadOnlyList<CompositeConfigurationVersionDetails>>, NotFound, ForbidHttpResult>> GetCompositeConfigurationVersions(
        string name,
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var versions = await compositeService.GetVersionsAsync(name, cancellationToken);
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
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var dto = await compositeService.GetVersionAsync(name, version, cancellationToken);
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
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await compositeService.PublishVersionAsync(name, version, cancellationToken);
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
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await compositeService.DeleteVersionAsync(name, version, cancellationToken);
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
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await compositeService.AddChildAsync(name, version, request, cancellationToken);
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
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await compositeService.UpdateChildAsync(childId, request, cancellationToken);
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
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await compositeService.RemoveChildAsync(childId, cancellationToken);
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

    private static async Task<Results<Ok<CompositeConfigurationItemDetails>, NotFound, BadRequest<ErrorResponse>, ForbidHttpResult>> UpdateChildConfigurationByItemId(
        Guid itemId,
        UpdateChildConfigurationRequest request,
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var item = await compositeService.UpdateChildAsync(itemId, request, cancellationToken);
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

    private static async Task<Results<NoContent, NotFound, BadRequest<ErrorResponse>, ForbidHttpResult>> RemoveChildConfigurationByItemId(
        Guid itemId,
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await compositeService.RemoveChildAsync(itemId, cancellationToken);
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

    private static async Task<Results<NoContent, NotFound, BadRequest<ErrorResponse>>> ReorderChildConfigurationByItemId(
        Guid itemId,
        int newOrder,
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await compositeService.ReorderChildAsync(itemId, newOrder, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<Ok<IReadOnlyList<PermissionEntry>>, NotFound, ForbidHttpResult>> GetCompositeConfigurationPermissions(
        string name,
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var permissions = await compositeService.GetPermissionsAsync(name, cancellationToken);
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
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await compositeService.GrantPermissionAsync(name, request, cancellationToken);
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
        PrincipalType principalType,
        Guid principalId,
        ICompositeConfigurationService compositeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await compositeService.RevokePermissionAsync(name, new RevokePermissionRequest { PrincipalId = principalId, PrincipalType = principalType }, cancellationToken);
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

