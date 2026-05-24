// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using OpenDsc.Contracts.Users;
using OpenDsc.Server.Authorization;

namespace OpenDsc.Server.Endpoints;

/// <summary>
/// Endpoints for user management.
/// </summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireAuthorization(ServerPermissions.UsersManage);

        group.MapGet("/", GetUsers)
            .WithSummary("List all users")
            .WithDescription("Returns a list of all users.");

        group.MapGet("/{id:guid}", GetUser)
            .WithSummary("Get user details")
            .WithDescription("Returns details for a specific user.");

        group.MapPost("/", CreateUser)
            .WithSummary("Create user")
            .WithDescription("Creates a new user account.");

        group.MapPut("/{id:guid}", UpdateUser)
            .WithSummary("Update user")
            .WithDescription("Updates user details.");

        group.MapDelete("/{id:guid}", DeleteUser)
            .WithSummary("Delete user")
            .WithDescription("Deletes a user account.");

        group.MapPost("/{id:guid}/reset-password", ResetPassword)
            .WithSummary("Reset password")
            .WithDescription("Resets a user's password and requires them to change it on next login.");

        group.MapPost("/{id:guid}/unlock", UnlockUser)
            .WithSummary("Unlock user")
            .WithDescription("Unlocks a locked user account.");

        group.MapGet("/{id:guid}/roles", GetUserRoles)
            .WithSummary("Get user roles")
            .WithDescription("Returns the roles assigned to a user.");

        group.MapGet("/{id:guid}/external-login", GetExternalLogin)
            .WithSummary("Get user external login")
            .WithDescription("Returns the external login provider configured for a user, if any.");

        group.MapGet("/{id:guid}/effective-permissions", GetEffectivePermissions)
            .WithSummary("Get user effective permissions")
            .WithDescription("Returns the effective permission set for a user.");

        group.MapGet("/counts/roles", GetUserRoleCounts)
            .WithSummary("Get user role counts")
            .WithDescription("Returns a map of user IDs to assigned role counts.");

        group.MapGet("/counts/groups", GetUserGroupCounts)
            .WithSummary("Get user group counts")
            .WithDescription("Returns a map of user IDs to assigned group counts.");

        group.MapPut("/{id:guid}/roles", SetUserRoles)
            .WithSummary("Set user roles")
            .WithDescription("Sets the roles for a user, replacing existing role assignments.");

        group.MapPost("/{id:guid}/roles", AssignRole)
            .WithSummary("Assign role to user")
            .WithDescription("Assigns a single role to a user.");

        group.MapDelete("/{id:guid}/roles/{roleId:guid}", RemoveRole)
            .WithSummary("Remove role from user")
            .WithDescription("Removes a single role from a user.");
    }

    private static async Task<Ok<List<UserSummary>>> GetUsers(
        IUserService service,
        CancellationToken cancellationToken)
    {
        return TypedResults.Ok((await service.GetUsersAsync(cancellationToken)).ToList());
    }

    private static async Task<Results<Ok<UserDetails>, NotFound>> GetUser(
        Guid id,
        IUserService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await service.GetUserAsync(id, cancellationToken);
            return TypedResults.Ok(user);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Created<UserSummary>, BadRequest<string>>> CreateUser(
        [FromBody] CreateUserRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await service.CreateUserAsync(request, cancellationToken);
            return TypedResults.Created($"/api/v1/users/{user.Id}", user);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<Ok<UserSummary>, NotFound, BadRequest<string>>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await service.UpdateUserAsync(id, request, cancellationToken);
            if (updated is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    private static async Task<Results<NoContent, NotFound>> DeleteUser(
        Guid id,
        IUserService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteUserAsync(id, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> ResetPassword(
        Guid id,
        [FromBody] ResetPasswordRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.ResetPasswordAsync(id, request, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Ok, NotFound>> UnlockUser(
        Guid id,
        IUserService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.UnlockUserAsync(id, cancellationToken);
            return TypedResults.Ok();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Ok<List<RoleSummary>>, NotFound>> GetUserRoles(
        Guid id,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var roles = await service.GetUserRolesAsync(id, cancellationToken);
        if (roles is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(roles.ToList());
    }

    private static async Task<Results<Ok<string>, NotFound>> GetExternalLogin(
        Guid id,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var provider = await service.GetExternalLoginAsync(id, cancellationToken);
        if (provider is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(provider);
    }

    private static async Task<Ok<HashSet<string>>> GetEffectivePermissions(
        Guid id,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var permissions = await service.GetEffectivePermissionsAsync(id, cancellationToken);
        return TypedResults.Ok(permissions);
    }

    private static async Task<Ok<IReadOnlyDictionary<Guid, int>>> GetUserRoleCounts(
        IUserService service,
        CancellationToken cancellationToken)
    {
        var counts = await service.GetUserRoleCountsAsync(cancellationToken);
        return TypedResults.Ok(counts);
    }

    private static async Task<Ok<IReadOnlyDictionary<Guid, int>>> GetUserGroupCounts(
        IUserService service,
        CancellationToken cancellationToken)
    {
        var counts = await service.GetUserGroupCountsAsync(cancellationToken);
        return TypedResults.Ok(counts);
    }

    private static async Task<Results<Ok, NotFound>> SetUserRoles(
        Guid id,
        [FromBody] SetUserRolesRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.SetUserRolesAsync(id, request, cancellationToken);
            return TypedResults.Ok();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Ok, NotFound>> AssignRole(
        Guid id,
        [FromBody] AssignRoleRequest request,
        IUserService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.AssignRoleAsync(id, request, cancellationToken);
            return TypedResults.Ok();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<NoContent, NotFound>> RemoveRole(
        Guid id,
        Guid roleId,
        IUserService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.RemoveRoleAsync(id, new RemoveRoleRequest { RoleId = roleId }, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }
}
