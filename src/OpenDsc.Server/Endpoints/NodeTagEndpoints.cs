// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

using OpenDsc.Contracts.Nodes;
using OpenDsc.Server.Authorization;

namespace OpenDsc.Server.Endpoints;

public static class NodeTagEndpoints
{
    public static IEndpointRouteBuilder MapNodeTagEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/nodes/{nodeId:guid}/tags")
            .WithTags("Node Tags")
            .RequireAuthorization(ScopePermissions.AdminOverride);

        group.MapGet("/", GetNodeTags)
            .WithName("GetNodeTags")
            .WithDescription("Get all scope value tags assigned to a node");

        group.MapPost("/", AssignNodeTag)
            .WithName("AssignNodeTag")
            .WithDescription("Assign a scope value tag to a node (only one value per scope type allowed)");

        group.MapDelete("/{scopeValueId:guid}", RemoveNodeTag)
            .WithName("RemoveNodeTag")
            .WithDescription("Remove a scope value tag from a node");

        return app;
    }

    private static async Task<Results<Ok<List<NodeTagSummary>>, NotFound>> GetNodeTags(
        Guid nodeId,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var tags = await nodeService.GetNodeTagsAsync(nodeId, cancellationToken);
            return TypedResults.Ok(tags.ToList());
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static async Task<Results<Created<NodeTagSummary>, BadRequest<string>, NotFound, Conflict<string>>> AssignNodeTag(
        Guid nodeId,
        [FromBody] AddNodeTagRequest request,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var tag = await nodeService.AddNodeTagAsync(nodeId, request, cancellationToken);
            return TypedResults.Created($"/api/v1/nodes/{nodeId}/tags/{tag.ScopeValueId}", tag);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
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

    private static async Task<Results<NoContent, NotFound>> RemoveNodeTag(
        Guid nodeId,
        Guid scopeValueId,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await nodeService.RemoveNodeTagAsync(
                nodeId,
                new RemoveNodeTagRequest { ScopeValueId = scopeValueId },
                cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound();
        }
    }
}
