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

        group.MapPut("/{id:guid}/roles", SetUserRoles)
            .WithSummary("Set user roles")
            .WithDescription("Sets the roles for a user, replacing existing role assignments.");
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
}
