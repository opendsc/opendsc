// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;

using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

using OpenDsc.Server.Contracts;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Endpoints;

public static class NodeEndpoints
{
    public static void MapNodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/nodes")
            .WithTags("Nodes");

        group.MapPost("/register", RegisterNode)
            .WithSummary("Register a node")
            .WithDescription("Registers a new node or re-registers an existing node with the server using mTLS.");

        group.MapGet("/", GetNodes)
            .RequireAuthorization("Admin")
            .WithSummary("List all nodes")
            .WithDescription("Returns a list of all registered nodes.");

        group.MapGet("/{nodeId:guid}", GetNode)
            .RequireAuthorization("Admin")
            .WithSummary("Get node details")
            .WithDescription("Returns details for a specific node.");

        group.MapDelete("/{nodeId:guid}", DeleteNode)
            .RequireAuthorization("Admin")
            .WithSummary("Delete a node")
            .WithDescription("Deletes a node and its associated reports.");

        group.MapGet("/{nodeId:guid}/configuration", GetNodeConfiguration)
            .RequireAuthorization("Node")
            .WithSummary("Get assigned configuration")
            .WithDescription("Downloads the configuration assigned to the node.");

        group.MapPut("/{nodeId:guid}/configuration", AssignConfiguration)
            .RequireAuthorization("Admin")
            .WithSummary("Assign configuration")
            .WithDescription("Assigns a configuration to a node by name.");

        group.MapGet("/{nodeId:guid}/configuration/checksum", GetConfigurationChecksum)
            .RequireAuthorization("Node")
            .WithSummary("Get configuration checksum")
            .WithDescription("Returns the checksum of the assigned configuration for change detection.");

        group.MapPost("/{nodeId:guid}/rotate-certificate", RotateCertificate)
            .RequireAuthorization("Node")
            .WithSummary("Rotate certificate")
            .WithDescription("Updates the node's certificate to a new one.");
    }

    private static async Task<Results<Ok<RegisterNodeResponse>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> RegisterNode(
        RegisterNodeRequest request,
        HttpContext httpContext,
        IWebHostEnvironment env,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Fqdn))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "FQDN is required." });
        }

        if (string.IsNullOrWhiteSpace(request.RegistrationKey))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Registration key is required." });
        }

        var clientCert = httpContext.Connection.ClientCertificate;
        string thumbprint;
        string subject;
        DateTime notAfter;

        if (env.IsEnvironment("Testing") && clientCert is null)
        {
            thumbprint = $"TEST-{Guid.NewGuid():N}";
            subject = $"CN={request.Fqdn}";
            notAfter = DateTime.UtcNow.AddYears(1);
        }
        else
        {
            if (clientCert is null)
            {
                return TypedResults.BadRequest(new ErrorResponse { Error = "Client certificate is required." });
            }

            thumbprint = clientCert.Thumbprint;
            if (string.IsNullOrEmpty(thumbprint))
            {
                return TypedResults.BadRequest(new ErrorResponse { Error = "Certificate thumbprint is invalid." });
            }

            subject = clientCert.Subject;
            notAfter = clientCert.NotAfter;
        }

        var registrationKey = await db.RegistrationKeys
            .FirstOrDefaultAsync(k => k.Key == request.RegistrationKey, cancellationToken);

        if (registrationKey is null)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Invalid registration key." });
        }

        if (registrationKey.IsRevoked)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Registration key has been revoked." });
        }

        if (DateTimeOffset.UtcNow > registrationKey.ExpiresAt)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Registration key has expired." });
        }

        if (registrationKey.MaxUses.HasValue && registrationKey.CurrentUses >= registrationKey.MaxUses.Value)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Registration key has reached its maximum uses." });
        }

        var existingNode = await db.Nodes.FirstOrDefaultAsync(n => n.Fqdn == request.Fqdn, cancellationToken);
        if (existingNode is not null)
        {
            existingNode.CertificateThumbprint = thumbprint;
            existingNode.CertificateSubject = subject;
            existingNode.CertificateNotAfter = notAfter;
            existingNode.LastCheckIn = DateTimeOffset.UtcNow;
            registrationKey.CurrentUses++;
            await db.SaveChangesAsync(cancellationToken);

            return TypedResults.Ok(new RegisterNodeResponse
            {
                NodeId = existingNode.Id
            });
        }

        var node = new Node
        {
            Id = Guid.NewGuid(),
            Fqdn = request.Fqdn,
            CertificateThumbprint = thumbprint,
            CertificateSubject = subject,
            CertificateNotAfter = notAfter,
            Status = NodeStatus.Unknown,
            CreatedAt = DateTimeOffset.UtcNow,
            LastCheckIn = DateTimeOffset.UtcNow
        };

        db.Nodes.Add(node);
        registrationKey.CurrentUses++;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new RegisterNodeResponse
        {
            NodeId = node.Id
        });
    }

    private static async Task<Ok<List<NodeSummary>>> GetNodes(
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var nodes = await db.Nodes
            .AsNoTracking()
            .Select(n => new NodeSummary
            {
                Id = n.Id,
                Fqdn = n.Fqdn,
                ConfigurationName = n.ConfigurationName,
                Status = n.Status.ToString(),
                LastCheckIn = n.LastCheckIn,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(nodes);
    }

    private static async Task<Results<Ok<NodeSummary>, NotFound<ErrorResponse>>> GetNode(
        Guid nodeId,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var node = await db.Nodes
            .AsNoTracking()
            .Where(n => n.Id == nodeId)
            .Select(n => new NodeSummary
            {
                Id = n.Id,
                Fqdn = n.Fqdn,
                ConfigurationName = n.ConfigurationName,
                Status = n.Status.ToString(),
                LastCheckIn = n.LastCheckIn,
                CreatedAt = n.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (node is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        return TypedResults.Ok(node);
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>> DeleteNode(
        Guid nodeId,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var node = await db.Nodes.FindAsync([nodeId], cancellationToken);
        if (node is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        db.Nodes.Remove(node);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<string>, NotFound<ErrorResponse>, ForbidHttpResult>> GetNodeConfiguration(
        Guid nodeId,
        ClaimsPrincipal user,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var authenticatedNodeId = user.FindFirst("node_id")?.Value;
        if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
        {
            return TypedResults.Forbid();
        }

        var node = await db.Nodes.FindAsync([nodeId], cancellationToken);
        if (node is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        node.LastCheckIn = DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(node.ConfigurationName))
        {
            await db.SaveChangesAsync(cancellationToken);
            return TypedResults.NotFound(new ErrorResponse { Error = "No configuration assigned." });
        }

        var config = await db.Configurations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Name == node.ConfigurationName, cancellationToken);

        if (config is null)
        {
            await db.SaveChangesAsync(cancellationToken);
            return TypedResults.NotFound(new ErrorResponse { Error = "Assigned configuration not found." });
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(config.Content);
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> AssignConfiguration(
        Guid nodeId,
        AssignConfigurationRequest request,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ConfigurationName))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Configuration name is required." });
        }

        var node = await db.Nodes.FindAsync([nodeId], cancellationToken);
        if (node is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        var configExists = await db.Configurations.AnyAsync(c => c.Name == request.ConfigurationName, cancellationToken);
        if (!configExists)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Configuration not found." });
        }

        node.ConfigurationName = request.ConfigurationName;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<ConfigurationChecksumResponse>, NotFound<ErrorResponse>, ForbidHttpResult>> GetConfigurationChecksum(
        Guid nodeId,
        ClaimsPrincipal user,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var authenticatedNodeId = user.FindFirst("node_id")?.Value;
        if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
        {
            return TypedResults.Forbid();
        }

        var node = await db.Nodes.FindAsync([nodeId], cancellationToken);
        if (node is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        node.LastCheckIn = DateTimeOffset.UtcNow;

        if (string.IsNullOrWhiteSpace(node.ConfigurationName))
        {
            await db.SaveChangesAsync(cancellationToken);
            return TypedResults.NotFound(new ErrorResponse { Error = "No configuration assigned." });
        }

        var checksum = await db.Configurations
            .AsNoTracking()
            .Where(c => c.Name == node.ConfigurationName)
            .Select(c => c.Checksum)
            .FirstOrDefaultAsync(cancellationToken);

        if (checksum is null)
        {
            await db.SaveChangesAsync(cancellationToken);
            return TypedResults.NotFound(new ErrorResponse { Error = "Assigned configuration not found." });
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(new ConfigurationChecksumResponse { Checksum = checksum });
    }

    private static async Task<Results<Ok<RotateCertificateResponse>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>, ForbidHttpResult>> RotateCertificate(
        Guid nodeId,
        RotateCertificateRequest request,
        ClaimsPrincipal user,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var authenticatedNodeId = user.FindFirst("node_id")?.Value;
        if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
        {
            return TypedResults.Forbid();
        }

        if (string.IsNullOrWhiteSpace(request.CertificateThumbprint))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Certificate thumbprint is required." });
        }

        if (string.IsNullOrWhiteSpace(request.CertificateSubject))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Certificate subject is required." });
        }

        var node = await db.Nodes.FindAsync([nodeId], cancellationToken);
        if (node is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        node.CertificateThumbprint = request.CertificateThumbprint;
        node.CertificateSubject = request.CertificateSubject;
        node.CertificateNotAfter = request.CertificateNotAfter;
        node.LastCheckIn = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new RotateCertificateResponse
        {
            Message = "Certificate updated successfully"
        });
    }
}
