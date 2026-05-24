// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using OpenDsc.Contracts.Users;
using OpenDsc.Server.Authorization;

namespace OpenDsc.Server.Endpoints;

/// <summary>
/// Endpoints for role management.
/// </summary>
public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/roles")
            .WithTags("Roles")
            .RequireAuthorization(ServerPermissions.RolesManage);

        group.MapGet("/", GetRoles)
            .WithSummary("List all roles")
            .WithDescription("Returns a list of all roles.");

        group.MapGet("/{id:guid}", GetRole)
            .WithSummary("Get role details")
            .WithDescription("Returns details for a specific role.");

        group.MapGet("/{id:guid}/groups", GetGroupsForRole)
            .WithSummary("Get groups for role")
            .WithDescription("Returns groups assigned to a role.");

        group.MapGet("/counts/users", GetRoleUserCounts)
            .WithSummary("Get role user counts")
            .WithDescription("Returns a map of role IDs to assigned user counts.");

        group.MapGet("/counts/groups", GetRoleGroupCounts)
            .WithSummary("Get role group counts")
            .WithDescription("Returns a map of role IDs to assigned group counts.");

        group.MapPost("/", CreateRole)
            .WithSummary("Create role")
            .WithDescription("Creates a new custom role.");

        group.MapPut("/{id:guid}", UpdateRole)
            .WithSummary("Update role")
            .WithDescription("Updates a custom role's details and permissions.");

        group.MapDelete("/{id:guid}", DeleteRole)
            .WithSummary("Delete role")
            .WithDescription("Deletes a custom role (system roles cannot be deleted).");

        group.MapPut("/{id:guid}/groups", SetGroupsForRole)
            .WithSummary("Set groups for role")
            .WithDescription("Sets the groups for a role, replacing existing group assignments.");
    }

    private static async Task<Ok<List<RoleSummary>>> GetRoles(
        IRoleService service,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok((await service.GetRolesAsync(cancellationToken)).ToList());
    }

    private static async Task<Results<Ok<RoleDetails>, NotFound>> GetRole(
        Guid id,
        IRoleService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var role = await service.GetRoleAsync(id, cancellationToken);
            return TypedResults.Ok(role);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Ok<List<GroupSummary>>, NotFound>> GetGroupsForRole(
        Guid id,
        IRoleService service,
        CancellationToken cancellationToken)
    {
        var groups = await service.GetGroupsForRoleAsync(id, cancellationToken);
        if (groups is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(groups.ToList());
    }

    private static async Task<Ok<IReadOnlyDictionary<Guid, int>>> GetRoleUserCounts(
        IRoleService service,
        CancellationToken cancellationToken)
    {
        var counts = await service.GetRoleUserCountsAsync(cancellationToken);
        return TypedResults.Ok(counts);
    }

    private static async Task<Ok<IReadOnlyDictionary<Guid, int>>> GetRoleGroupCounts(
        IRoleService service,
        CancellationToken cancellationToken)
    {
        var counts = await service.GetRoleGroupCountsAsync(cancellationToken);
        return TypedResults.Ok(counts);
    }

    private static async Task<Results<Created<RoleSummary>, BadRequest<string>>> CreateRole(
        [FromBody] CreateRoleRequest request,
        IRoleService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var role = await service.CreateRoleAsync(request, cancellationToken);
            return TypedResults.Created($"/api/v1/roles/{role.Id}", role);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok<RoleSummary>, NotFound, BadRequest<string>>> UpdateRole(
        Guid id,
        [FromBody] UpdateRoleRequest request,
        IRoleService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var role = await service.UpdateRoleAsync(id, request, cancellationToken);
            return TypedResults.Ok(role);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> DeleteRole(
        Guid id,
        IRoleService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteRoleAsync(id, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok, NotFound>> SetGroupsForRole(
        Guid id,
        [FromBody] SetRoleGroupsRequest request,
        IRoleService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.SetGroupsForRoleAsync(id, request, cancellationToken);
            return TypedResults.Ok();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }
}
