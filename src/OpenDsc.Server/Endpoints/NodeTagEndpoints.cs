// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Endpoints;

public static class NodeTagEndpoints
{
    public static IEndpointRouteBuilder MapNodeTagEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/nodes/{nodeId:guid}/tags")
            .WithTags("Node Tags")
            .RequireAuthorization(Permissions.Scopes_AdminOverride);

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

    private static async Task<Results<Ok<List<NodeTagDto>>, NotFound>> GetNodeTags(
        Guid nodeId,
        ServerDbContext db)
    {
        var node = await db.Nodes.FindAsync(nodeId);
        if (node is null)
        {
            return TypedResults.NotFound();
        }

        var tags = await db.NodeTags
            .Include(nt => nt.ScopeValue)
            .ThenInclude(sv => sv.ScopeType)
            .Where(nt => nt.NodeId == nodeId)
            .OrderBy(nt => nt.ScopeValue.ScopeType.Precedence)
            .Select(nt => new NodeTagDto
            {
                NodeId = nt.NodeId,
                ScopeValueId = nt.ScopeValueId,
                ScopeTypeName = nt.ScopeValue.ScopeType.Name,
                ScopeValue = nt.ScopeValue.Value,
                Precedence = nt.ScopeValue.ScopeType.Precedence,
                AssignedAt = nt.AssignedAt
            })
            .ToListAsync();

        return TypedResults.Ok(tags);
    }

    private static async Task<Results<Created<NodeTagDto>, BadRequest<string>, NotFound, Conflict<string>>> AssignNodeTag(
        Guid nodeId,
        [FromBody] AssignNodeTagRequest request,
        ServerDbContext db)
    {
        var node = await db.Nodes.FindAsync(nodeId);
        if (node is null)
        {
            return TypedResults.NotFound();
        }

        var scopeValue = await db.ScopeValues
            .Include(sv => sv.ScopeType)
            .FirstOrDefaultAsync(sv => sv.Id == request.ScopeValueId);

        if (scopeValue is null)
        {
            return TypedResults.BadRequest("Scope value not found");
        }

        var existingTagWithSameType = await db.NodeTags
            .Include(nt => nt.ScopeValue)
            .FirstOrDefaultAsync(nt =>
                nt.NodeId == nodeId &&
                nt.ScopeValue.ScopeTypeId == scopeValue.ScopeTypeId);

        if (existingTagWithSameType != null)
        {
            return TypedResults.Conflict($"Node already has a tag for scope type '{scopeValue.ScopeType.Name}'. Remove the existing tag first.");
        }

        var existingTag = await db.NodeTags
            .FirstOrDefaultAsync(nt => nt.NodeId == nodeId && nt.ScopeValueId == request.ScopeValueId);

        if (existingTag != null)
        {
            return TypedResults.Conflict("This scope value is already assigned to the node");
        }

        var nodeTag = new NodeTag
        {
            NodeId = nodeId,
            ScopeValueId = request.ScopeValueId,
            AssignedAt = DateTimeOffset.UtcNow
        };

        db.NodeTags.Add(nodeTag);
        await db.SaveChangesAsync();

        var dto = new NodeTagDto
        {
            NodeId = nodeTag.NodeId,
            ScopeValueId = nodeTag.ScopeValueId,
            ScopeTypeName = scopeValue.ScopeType.Name,
            ScopeValue = scopeValue.Value,
            Precedence = scopeValue.ScopeType.Precedence,
            AssignedAt = nodeTag.AssignedAt
        };

        return TypedResults.Created($"/api/v1/nodes/{nodeId}/tags/{scopeValue.Id}", dto);
    }

    private static async Task<Results<NoContent, NotFound>> RemoveNodeTag(
        Guid nodeId,
        Guid scopeValueId,
        ServerDbContext db)
    {
        var nodeTag = await db.NodeTags
            .FirstOrDefaultAsync(nt => nt.NodeId == nodeId && nt.ScopeValueId == scopeValueId);

        if (nodeTag is null)
        {
            return TypedResults.NotFound();
        }

        db.NodeTags.Remove(nodeTag);
        await db.SaveChangesAsync();

        return TypedResults.NoContent();
    }
}

public sealed class NodeTagDto
{
    public required Guid NodeId { get; init; }
    public required Guid ScopeValueId { get; init; }
    public required string ScopeTypeName { get; init; }
    public required string ScopeValue { get; init; }
    public required int Precedence { get; init; }
    public required DateTimeOffset AssignedAt { get; init; }
}

public sealed class AssignNodeTagRequest
{
    public required Guid ScopeValueId { get; init; }
}
