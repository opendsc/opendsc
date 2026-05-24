// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;

using Microsoft.AspNetCore.Http.HttpResults;

using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Server.Authorization;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Server.Endpoints;

public static class NodeEndpoints
{
    public static void MapNodeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/nodes")
            .RequireAuthorization()
            .WithTags("Nodes");

        group.MapPost("/register", RegisterNode)
            .AllowAnonymous()
            .WithSummary("Register a node")
            .WithDescription("Registers a new node or re-registers an existing node with the server using mTLS.");

        group.MapGet("/", GetNodes)
            .RequireAuthorization(NodePermissions.Read)
            .WithSummary("List all nodes")
            .WithDescription("Returns a list of all registered nodes.");

        group.MapGet("/{nodeId:guid}", GetNode)
            .RequireAuthorization(NodePermissions.Read)
            .WithSummary("Get node details")
            .WithDescription("Returns details for a specific node.");

        group.MapDelete("/{nodeId:guid}", DeleteNode)
            .RequireAuthorization(NodePermissions.Delete)
            .WithSummary("Delete a node")
            .WithDescription("Deletes a node and its associated reports.");

        group.MapGet("/{nodeId:guid}/configuration", GetNodeConfiguration)
            .RequireAuthorization("Node")
            .WithSummary("Get assigned configuration")
            .WithDescription("Downloads the configuration assigned to the node.");

        group.MapGet("/{nodeId:guid}/configuration/manifest", GetNodeConfigurationManifest)
            .RequireAuthorization(NodePermissions.Read)
            .WithSummary("Get node configuration manifest")
            .WithDescription("Returns manifest metadata and content for the node's assigned configuration.");

        group.MapGet("/{nodeId:guid}/assignment", GetNodeAssignment)
            .RequireAuthorization(NodePermissions.Read)
            .WithSummary("Get node assignment")
            .WithDescription("Returns the current configuration assignment summary for a node.");

        group.MapGet("/available-configurations", GetAvailableConfigurations)
            .RequireAuthorization(NodePermissions.Read)
            .WithSummary("Get available configurations")
            .WithDescription("Returns configurations that can be assigned to a node.");

        group.MapGet("/available-composite-configurations", GetAvailableCompositeConfigurations)
            .RequireAuthorization(NodePermissions.Read)
            .WithSummary("Get available composite configurations")
            .WithDescription("Returns composite configurations that can be assigned to a node.");

        group.MapGet("/assignable-configurations", GetAssignableConfigurations)
            .RequireAuthorization(NodePermissions.Read)
            .WithSummary("Get assignable configurations")
            .WithDescription("Returns regular configurations with available major versions.");

        group.MapGet("/assignable-composite-configurations", GetAssignableCompositeConfigurations)
            .RequireAuthorization(NodePermissions.Read)
            .WithSummary("Get assignable composite configurations")
            .WithDescription("Returns composite configurations with available major versions.");

        group.MapPut("/{nodeId:guid}/configuration", AssignConfiguration)
            .RequireAuthorization(NodePermissions.AssignConfiguration)
            .WithSummary("Assign configuration")
            .WithDescription("Assigns a configuration to a node by name.");

        group.MapDelete("/{nodeId:guid}/configuration", UnassignConfiguration)
            .RequireAuthorization(NodePermissions.AssignConfiguration)
            .WithSummary("Unassign configuration")
            .WithDescription("Removes the configuration assignment from a node.");

        group.MapGet("/{nodeId:guid}/configuration/checksum", GetConfigurationChecksum)
            .RequireAuthorization("Node")
            .WithSummary("Get configuration checksum")
            .WithDescription("Returns the checksum of the assigned configuration for change detection.");

        group.MapGet("/{nodeId:guid}/configuration/bundle", GetConfigurationBundle)
            .RequireAuthorization("Node")
            .WithSummary("Download configuration bundle")
            .WithDescription("Downloads a ZIP bundle containing the configuration files and merged parameters.");

        group.MapGet("/{nodeId:guid}/configuration/bundle/download", DownloadConfigurationBundle)
            .RequireAuthorization(NodePermissions.Read)
            .WithSummary("Download node configuration bundle")
            .WithDescription("Downloads a ZIP bundle containing the configuration files and merged parameters for admin/read clients.");

        group.MapPost("/{nodeId:guid}/rotate-certificate", RotateCertificate)
            .RequireAuthorization("Node")
            .WithSummary("Rotate certificate")
            .WithDescription("Updates the node's certificate to a new one.");

        group.MapPut("/{nodeId:guid}/lcm-status", UpdateLcmStatus)
            .RequireAuthorization("Node")
            .WithSummary("Update LCM status")
            .WithDescription("Updates the node's current LCM operational status.");

        group.MapGet("/{nodeId:guid}/status-history", GetNodeStatusHistory)
            .RequireAuthorization(NodePermissions.Read)
            .WithSummary("Get node status history")
            .WithDescription("Returns the LCM and compliance status event history for a node.");

        group.MapGet("/{nodeId:guid}/scope-values", GetNodeScopeValues)
            .RequireAuthorization(NodePermissions.Read)
            .WithSummary("Get node scope values")
            .WithDescription("Returns the scope values currently associated with a node.");

        group.MapGet("/{nodeId:guid}/lcm-config", GetNodeLcmConfig)
            .RequireAuthorization("Node")
            .WithSummary("Get desired LCM configuration")
            .WithDescription("Returns the server-managed desired LCM configuration for the node.");

        group.MapPut("/{nodeId:guid}/lcm-config", UpdateNodeLcmConfig)
            .RequireAuthorization(NodePermissions.Write)
            .WithSummary("Update desired LCM configuration")
            .WithDescription("Updates the server-managed desired LCM configuration for the node.");

        group.MapPut("/{nodeId:guid}/scope-values", SetNodeScopeValue)
            .RequireAuthorization(NodePermissions.Write)
            .WithSummary("Set node scope value")
            .WithDescription("Creates or updates the scope value associated with a node for a scope type.");

        group.MapPut("/{nodeId:guid}/reported-config", ReportNodeLcmConfig)
            .RequireAuthorization("Node")
            .WithSummary("Report current LCM configuration")
            .WithDescription("Called by the node to report its current LCM configuration to the server.");
    }

    private static async Task<Results<Ok<RegisterNodeResponse>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> RegisterNode(
        RegisterNodeRequest request,
        HttpContext httpContext,
        IWebHostEnvironment env,
        INodeService nodeService,
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

        if (env.IsEnvironment("Testing"))
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

        try
        {
            var response = await nodeService.RegisterNodeAsync(
                request,
                thumbprint,
                subject,
                notAfter,
                cancellationToken);

            return TypedResults.Ok(response);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return ex.Message.Contains("already registered to another node", StringComparison.OrdinalIgnoreCase)
                ? TypedResults.Conflict(new ErrorResponse { Error = ex.Message })
                : TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Ok<List<NodeSummary>>> GetNodes(
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        var nodes = await nodeService.GetNodesAsync(cancellationToken: cancellationToken);
        return TypedResults.Ok(nodes.ToList());
    }

    private static async Task<Results<Ok<NodeDetails>, NotFound<ErrorResponse>>> GetNode(
        Guid nodeId,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        var node = await nodeService.GetNodeAsync(nodeId, cancellationToken);
        if (node is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        return TypedResults.Ok(node);
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>> DeleteNode(
        Guid nodeId,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await nodeService.DeleteNodeAsync(nodeId, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }

    private static async Task<Results<Ok<string>, NotFound<ErrorResponse>, ForbidHttpResult>> GetNodeConfiguration(
        Guid nodeId,
        ClaimsPrincipal user,
        IWebHostEnvironment env,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        if (!env.IsEnvironment("Testing"))
        {
            var authenticatedNodeId = user.FindFirst("node_id")?.Value;
            if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
            {
                return TypedResults.Forbid();
            }
        }

        var manifest = await nodeService.GetNodeConfigurationManifestAsync(nodeId, cancellationToken);
        if (manifest is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "No configuration assigned." });
        }

        return TypedResults.Ok(manifest.Content);
    }

    private static async Task<Ok<NodeAssignmentSummary?>> GetNodeAssignment(
        Guid nodeId,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        NodeAssignmentSummary? assignment = await nodeService.GetNodeAssignmentAsync(nodeId, cancellationToken);
        return TypedResults.Ok<NodeAssignmentSummary?>(assignment);
    }

    private static async Task<Results<Ok<NodeConfigurationManifest>, NotFound<ErrorResponse>>> GetNodeConfigurationManifest(
        Guid nodeId,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        var manifest = await nodeService.GetNodeConfigurationManifestAsync(nodeId, cancellationToken);
        if (manifest is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "No configuration assigned." });
        }

        return TypedResults.Ok(manifest);
    }

    private static async Task<Ok<List<ConfigurationOption>>> GetAvailableConfigurations(
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        var configurations = await nodeService.GetAvailableConfigurationsAsync(cancellationToken);
        return TypedResults.Ok(configurations.ToList());
    }

    private static async Task<Ok<List<ConfigurationOption>>> GetAvailableCompositeConfigurations(
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        var configurations = await nodeService.GetAvailableCompositeConfigurationsAsync(cancellationToken);
        return TypedResults.Ok(configurations.ToList());
    }

    private static async Task<Ok<List<ConfigurationAssignmentOption>>> GetAssignableConfigurations(
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        var configurations = await nodeService.GetAssignableConfigurationsAsync(cancellationToken);
        return TypedResults.Ok(configurations.ToList());
    }

    private static async Task<Ok<List<ConfigurationAssignmentOption>>> GetAssignableCompositeConfigurations(
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        var configurations = await nodeService.GetAssignableCompositeConfigurationsAsync(cancellationToken);
        return TypedResults.Ok(configurations.ToList());
    }

    private static async Task<Results<FileStreamHttpResult, NotFound<ErrorResponse>>> GetConfigurationBundle(
        Guid nodeId,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        var bundle = await nodeService.GetNodeConfigurationBundleAsync(nodeId, cancellationToken);
        if (bundle is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "No configuration assigned." });
        }

        return TypedResults.File(new MemoryStream(bundle.Content), bundle.ContentType, bundle.FileName);
    }

    private static async Task<Results<FileStreamHttpResult, NotFound<ErrorResponse>>> DownloadConfigurationBundle(
        Guid nodeId,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        var bundle = await nodeService.GetNodeConfigurationBundleAsync(nodeId, cancellationToken);
        if (bundle is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "No configuration assigned." });
        }

        return TypedResults.File(new MemoryStream(bundle.Content), bundle.ContentType, bundle.FileName);
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> AssignConfiguration(
        Guid nodeId,
        AssignConfigurationRequest request,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ConfigurationName))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Configuration name is required." });
        }

        try
        {
            await nodeService.AssignConfigurationAsync(nodeId, request, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
        catch (InvalidOperationException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>>> UnassignConfiguration(
        Guid nodeId,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await nodeService.RemoveConfigurationAsync(nodeId, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }

    private static async Task<Results<Ok<ConfigurationChecksumResponse>, NotFound<ErrorResponse>, ForbidHttpResult>> GetConfigurationChecksum(
        Guid nodeId,
        ClaimsPrincipal user,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        var authenticatedNodeId = user.FindFirst("node_id")?.Value;
        if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
        {
            return TypedResults.Forbid();
        }

        var response = await nodeService.GetConfigurationChecksumAsync(nodeId, cancellationToken);
        if (response is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>, ForbidHttpResult>> UpdateLcmStatus(
        Guid nodeId,
        UpdateLcmStatusRequest request,
        ClaimsPrincipal user,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        var authenticatedNodeId = user.FindFirst("node_id")?.Value;
        if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
        {
            return TypedResults.Forbid();
        }

        try
        {
            await nodeService.UpdateLcmStatusAsync(nodeId, request, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }

    private static async Task<Results<Ok<List<NodeStatusEventSummary>>, NotFound<ErrorResponse>>> GetNodeStatusHistory(
        Guid nodeId,
        INodeService nodeService,
        int? skip,
        int? take,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        try
        {
            var events = await nodeService.GetNodeStatusEventsAsync(nodeId, cancellationToken);
            var filtered = events
                .Where(e => from == null || e.Timestamp >= from)
                .Where(e => to == null || e.Timestamp <= to)
                .Skip(skip ?? 0)
                .Take(take ?? 50)
                .ToList();

            return TypedResults.Ok(filtered);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }

    private static async Task<Results<Ok<List<NodeScopeValueSummary>>, NotFound<ErrorResponse>>> GetNodeScopeValues(
        Guid nodeId,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var scopeValues = await nodeService.GetNodeScopeValuesAsync(nodeId, cancellationToken);
            return TypedResults.Ok(scopeValues.ToList());
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }

    private static async Task<Results<NoContent, BadRequest<ErrorResponse>, NotFound<ErrorResponse>>> SetNodeScopeValue(
        Guid nodeId,
        SetNodeScopeValueRequest request,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        try
        {
            await nodeService.SetNodeScopeValueAsync(nodeId, request, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = ex.Message });
        }
    }

    private static async Task<Results<Ok<RotateCertificateResponse>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>, ForbidHttpResult>> RotateCertificate(
        Guid nodeId,
        RotateCertificateRequest request,
        ClaimsPrincipal user,
        INodeService nodeService,
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

        try
        {
            var response = await nodeService.RotateCertificateAsync(nodeId, request, cancellationToken);
            return TypedResults.Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }

    private static async Task<Results<Ok<NodeLcmConfigResponse>, NotFound<ErrorResponse>, ForbidHttpResult>> GetNodeLcmConfig(
        Guid nodeId,
        ClaimsPrincipal user,
        IWebHostEnvironment env,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        if (!env.IsEnvironment("Testing"))
        {
            var authenticatedNodeId = user.FindFirst("node_id")?.Value;
            if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
            {
                return TypedResults.Forbid();
            }
        }

        try
        {
            var response = await nodeService.GetNodeLcmConfigAsync(nodeId, cancellationToken);
            if (response is null)
            {
                return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
            }

            return TypedResults.Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }

    private static async Task<Results<Ok<NodeLcmConfigResponse>, NotFound<ErrorResponse>>> UpdateNodeLcmConfig(
        Guid nodeId,
        UpdateNodeLcmConfigRequest request,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await nodeService.UpdateNodeLcmConfigAsync(nodeId, request, cancellationToken);
            if (response is null)
            {
                return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
            }

            return TypedResults.Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }

    private static async Task<Results<NoContent, NotFound<ErrorResponse>, ForbidHttpResult>> ReportNodeLcmConfig(
        Guid nodeId,
        ReportNodeLcmConfigRequest request,
        ClaimsPrincipal user,
        IWebHostEnvironment env,
        INodeService nodeService,
        CancellationToken cancellationToken)
    {
        if (!env.IsEnvironment("Testing"))
        {
            var authenticatedNodeId = user.FindFirst("node_id")?.Value;
            if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
            {
                return TypedResults.Forbid();
            }
        }

        try
        {
            await nodeService.ReportNodeLcmConfigAsync(nodeId, request, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }
}
