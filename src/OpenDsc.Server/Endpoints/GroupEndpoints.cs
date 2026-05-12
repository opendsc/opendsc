// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using OpenDsc.Contracts.Users;
using OpenDsc.Server.Authorization;

namespace OpenDsc.Server.Endpoints;

/// <summary>
/// Endpoints for group management.
/// </summary>
public static class GroupEndpoints
{
    public static void MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/groups")
            .WithTags("Groups")
            .RequireAuthorization(ServerPermissions.GroupsManage);

        group.MapGet("/", GetGroups)
            .WithSummary("List all groups")
            .WithDescription("Returns a list of all groups.");

        group.MapGet("/{id:guid}", GetGroup)
            .WithSummary("Get group details")
            .WithDescription("Returns details for a specific group.");

        group.MapPost("/", CreateGroup)
            .WithSummary("Create group")
            .WithDescription("Creates a new group.");

        group.MapPut("/{id:guid}", UpdateGroup)
            .WithSummary("Update group")
            .WithDescription("Updates group details.");

        group.MapDelete("/{id:guid}", DeleteGroup)
            .WithSummary("Delete group")
            .WithDescription("Deletes a group.");

        group.MapGet("/{id:guid}/members", GetGroupMembers)
            .WithSummary("Get group members")
            .WithDescription("Returns the users who are members of this group.");

        group.MapPut("/{id:guid}/members", SetGroupMembers)
            .WithSummary("Set group members")
            .WithDescription("Sets the members of a group, replacing existing memberships.");

        group.MapGet("/{id:guid}/roles", GetGroupRoles)
            .WithSummary("Get group roles")
            .WithDescription("Returns the roles assigned to a group.");

        group.MapPut("/{id:guid}/roles", SetGroupRoles)
            .WithSummary("Set group roles")
            .WithDescription("Sets the roles for a group, replacing existing role assignments.");

        group.MapGet("/external-mappings", GetExternalGroupMappings)
            .WithSummary("List external group mappings")
            .WithDescription("Returns all external group mappings for SSO integration.");

        group.MapPost("/external-mappings", CreateExternalGroupMapping)
            .WithSummary("Create external group mapping")
            .WithDescription("Maps an external SSO group to an internal group.");

        group.MapDelete("/external-mappings/{id:guid}", DeleteExternalGroupMapping)
            .WithSummary("Delete external group mapping")
            .WithDescription("Removes an external group mapping.");
    }

    private static async Task<Ok<List<GroupSummary>>> GetGroups(
        IGroupService service,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok((await service.GetGroupsAsync(cancellationToken)).ToList());
    }

    private static async Task<Results<Ok<GroupDetails>, NotFound>> GetGroup(
        Guid id,
        IGroupService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var group = await service.GetGroupAsync(id, cancellationToken);
            return TypedResults.Ok(group);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Created<GroupSummary>, BadRequest<string>>> CreateGroup(
        [FromBody] CreateGroupRequest request,
        IGroupService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var group = await service.CreateGroupAsync(request, cancellationToken);
            return TypedResults.Created($"/api/v1/groups/{group.Id}", group);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok<GroupSummary>, NotFound, BadRequest<string>>> UpdateGroup(
        Guid id,
        [FromBody] UpdateGroupRequest request,
        IGroupService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var group = await service.UpdateGroupAsync(id, request, cancellationToken);
            return TypedResults.Ok(group);
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

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> DeleteGroup(
        Guid id,
        IGroupService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteGroupAsync(id, cancellationToken);
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

    private static async Task<Results<Ok<List<UserSummary>>, NotFound>> GetGroupMembers(
        Guid id,
        IGroupService service,
        CancellationToken cancellationToken)
    {
        var users = await service.GetGroupMembersAsync(id, cancellationToken);
        if (users is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(users.ToList());
    }

    private static async Task<Results<Ok, NotFound>> SetGroupMembers(
        Guid id,
        [FromBody] SetGroupMembersRequest request,
        IGroupService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.SetMembersAsync(id, request, cancellationToken);
            return TypedResults.Ok();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Ok<List<RoleSummary>>, NotFound>> GetGroupRoles(
        Guid id,
        IGroupService service,
        CancellationToken cancellationToken)
    {
        var roles = await service.GetGroupRolesAsync(id, cancellationToken);
        if (roles is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(roles.ToList());
    }

    private static async Task<Results<Ok, NotFound>> SetGroupRoles(
        Guid id,
        [FromBody] SetGroupRolesRequest request,
        IGroupService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.SetRolesAsync(id, request, cancellationToken);
            return TypedResults.Ok();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Ok<List<ExternalGroupMappingInfo>>> GetExternalGroupMappings(
        IGroupService service,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok((await service.GetExternalGroupMappingsAsync(cancellationToken)).ToList());
    }

    private static async Task<Results<Created<ExternalGroupMappingInfo>, NotFound, BadRequest<string>>> CreateExternalGroupMapping(
        [FromBody] CreateExternalGroupMappingRequest request,
        IGroupService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var mapping = await service.CreateExternalGroupMappingAsync(request, cancellationToken);
            if (mapping is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Created($"/api/v1/groups/external-mappings/{mapping.Id}", mapping);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok, NotFound>> DeleteExternalGroupMapping(
        Guid id,
        IGroupService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteExternalGroupMappingAsync(id, cancellationToken);
            return TypedResults.Ok();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }
}
