// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;

using Microsoft.AspNetCore.Http.HttpResults;

using OpenDsc.Contracts.Lcm;
using OpenDsc.Server.Authorization;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Server.Endpoints;

public sealed partial class NodeEndpoints(ILogger<NodeEndpoints> logger)
{
    public void MapNodeEndpoints(IEndpointRouteBuilder app)
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

        group.MapGet("/{nodeId:guid}/lcm-config", GetNodeLcmConfig)
            .RequireAuthorization("Node")
            .WithSummary("Get desired LCM configuration")
            .WithDescription("Returns the server-managed desired LCM configuration for the node.");

        group.MapPut("/{nodeId:guid}/lcm-config", UpdateNodeLcmConfig)
            .RequireAuthorization(NodePermissions.Write)
            .WithSummary("Update desired LCM configuration")
            .WithDescription("Updates the server-managed desired LCM configuration for the node.");

        group.MapPut("/{nodeId:guid}/reported-config", ReportNodeLcmConfig)
            .RequireAuthorization("Node")
            .WithSummary("Report current LCM configuration")
            .WithDescription("Called by the node to report its current LCM configuration to the server.");
    }

    private async Task<Results<Ok<RegisterNodeResponse>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> RegisterNode(
        RegisterNodeRequest request,
        HttpContext httpContext,
        IWebHostEnvironment env,
        INodeRegistrationManager registrationManager,
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
            var response = await registrationManager.RegisterNodeAsync(
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

    private async Task<Ok<List<NodeSummary>>> GetNodes(
        INodeReader nodeReader,
        CancellationToken cancellationToken)
    {
        var nodes = await nodeReader.GetNodesAsync(cancellationToken: cancellationToken);
        return TypedResults.Ok(nodes.ToList());
    }

    private async Task<Results<Ok<NodeDetails>, NotFound<ErrorResponse>>> GetNode(
        Guid nodeId,
        INodeReader nodeReader,
        CancellationToken cancellationToken)
    {
        var node = await nodeReader.GetNodeAsync(nodeId, cancellationToken);
        if (node is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        return TypedResults.Ok(node);
    }

    private async Task<Results<NoContent, NotFound<ErrorResponse>>> DeleteNode(
        Guid nodeId,
        INodeManager nodeManager,
        CancellationToken cancellationToken)
    {
        try
        {
            await nodeManager.DeleteNodeAsync(nodeId, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }

    private async Task<Results<Ok<string>, NotFound<ErrorResponse>, ForbidHttpResult>> GetNodeConfiguration(
        Guid nodeId,
        ClaimsPrincipal user,
        IWebHostEnvironment env,
        INodeConfigurationManager configurationManager,
        CancellationToken cancellationToken)
    {
        LogGettingNodeConfiguration(nodeId);
        try
        {
            if (!env.IsEnvironment("Testing"))
            {
                var authenticatedNodeId = user.FindFirst("node_id")?.Value;
                if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
                {
                    return TypedResults.Forbid();
                }
            }

            var manifest = await configurationManager.GetNodeConfigurationManifestAsync(nodeId, cancellationToken);
            if (manifest is null)
            {
                LogNoConfigurationFoundForNode(nodeId);
                return TypedResults.NotFound(new ErrorResponse { Error = "No configuration assigned." });
            }

            LogReturningNodeConfiguration(manifest.Content);
            return TypedResults.Ok(manifest.Content);
        }
        catch
        {
            throw;
        }
    }

    private async Task<Results<FileStreamHttpResult, NotFound<ErrorResponse>>> GetConfigurationBundle(
        Guid nodeId,
        INodeConfigurationManager configurationManager,
        CancellationToken cancellationToken)
    {
        var bundle = await configurationManager.GetNodeConfigurationBundleAsync(nodeId, cancellationToken);
        if (bundle is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "No configuration assigned." });
        }

        return TypedResults.File(new MemoryStream(bundle.Content), bundle.ContentType, bundle.FileName);
    }

    private async Task<Results<NoContent, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> AssignConfiguration(
        Guid nodeId,
        AssignConfigurationRequest request,
        INodeConfigurationManager configurationManager,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ConfigurationName))
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = "Configuration name is required." });
        }

        try
        {
            await configurationManager.AssignConfigurationAsync(nodeId, request, cancellationToken);
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

    private async Task<Results<NoContent, NotFound<ErrorResponse>>> UnassignConfiguration(
        Guid nodeId,
        INodeConfigurationManager configurationManager,
        CancellationToken cancellationToken)
    {
        try
        {
            await configurationManager.RemoveConfigurationAsync(nodeId, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }

    private async Task<Results<Ok<ConfigurationChecksumResponse>, NotFound<ErrorResponse>, ForbidHttpResult>> GetConfigurationChecksum(
        Guid nodeId,
        ClaimsPrincipal user,
        INodeConfigurationManager configurationManager,
        CancellationToken cancellationToken)
    {
        var authenticatedNodeId = user.FindFirst("node_id")?.Value;
        if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
        {
            return TypedResults.Forbid();
        }

        var response = await configurationManager.GetConfigurationChecksumAsync(nodeId, cancellationToken);
        if (response is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
        return TypedResults.Ok(response);
    }

    private async Task<Results<NoContent, NotFound<ErrorResponse>, ForbidHttpResult>> UpdateLcmStatus(
        Guid nodeId,
        UpdateLcmStatusRequest request,
        ClaimsPrincipal user,
        INodeLcmManager lcmManager,
        CancellationToken cancellationToken)
    {
        var authenticatedNodeId = user.FindFirst("node_id")?.Value;
        if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
        {
            return TypedResults.Forbid();
        }

        try
        {
            await lcmManager.UpdateLcmStatusAsync(nodeId, request, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }

    private async Task<Results<Ok<List<NodeStatusEventSummary>>, NotFound<ErrorResponse>>> GetNodeStatusHistory(
        Guid nodeId,
        INodeReader nodeReader,
        int? skip,
        int? take,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        try
        {
            var events = await nodeReader.GetNodeStatusEventsAsync(nodeId, cancellationToken);
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

    private async Task<Results<Ok<RotateCertificateResponse>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>, ForbidHttpResult>> RotateCertificate(
        Guid nodeId,
        RotateCertificateRequest request,
        ClaimsPrincipal user,
        INodeLcmManager lcmManager,
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
            var response = await lcmManager.RotateCertificateAsync(nodeId, request, cancellationToken);
            return TypedResults.Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }

    private async Task<Results<Ok<NodeLcmConfigResponse>, NotFound<ErrorResponse>, ForbidHttpResult>> GetNodeLcmConfig(
        Guid nodeId,
        ClaimsPrincipal user,
        IWebHostEnvironment env,
        INodeLcmManager lcmManager,
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
            var response = await lcmManager.GetNodeLcmConfigAsync(nodeId, cancellationToken);
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

    private async Task<Results<Ok<NodeLcmConfigResponse>, NotFound<ErrorResponse>>> UpdateNodeLcmConfig(
        Guid nodeId,
        UpdateNodeLcmConfigRequest request,
        INodeLcmManager lcmManager,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await lcmManager.UpdateNodeLcmConfigAsync(nodeId, request, cancellationToken);
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

    private async Task<Results<NoContent, NotFound<ErrorResponse>, ForbidHttpResult>> ReportNodeLcmConfig(
        Guid nodeId,
        ReportNodeLcmConfigRequest request,
        ClaimsPrincipal user,
        IWebHostEnvironment env,
        INodeLcmManager lcmManager,
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
            await lcmManager.ReportNodeLcmConfigAsync(nodeId, request, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (KeyNotFoundException)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }
    }

    [LoggerMessage(EventId = EventIds.GettingNodeConfiguration, Level = LogLevel.Debug, Message = "GetNodeConfiguration called for nodeId: {NodeId}")]
    private partial void LogGettingNodeConfiguration(Guid nodeId);

    [LoggerMessage(EventId = EventIds.NodeConfigurationFound, Level = LogLevel.Debug, Message = "NodeConfigurations lookup: nodeConfig is {Found}")]
    private partial void LogNodeConfigurationFound(bool found);

    [LoggerMessage(EventId = EventIds.NodeConfigurationContentSetFromAssignment, Level = LogLevel.Debug, Message = "Set configContent from NodeConfigurations")]
    private partial void LogNodeConfigurationContentSetFromAssignment();

    [LoggerMessage(EventId = EventIds.NodeConfigurationFallback, Level = LogLevel.Debug, Message = "Fallback: node found: {NodeFound}, ConfigurationName: {ConfigurationName}")]
    private partial void LogNodeConfigurationFallback(bool nodeFound, string? configurationName);

    [LoggerMessage(EventId = EventIds.LookingForConfigurationByName, Level = LogLevel.Debug, Message = "Looking for config with name: {ConfigName}")]
    private partial void LogLookingForConfigurationByName(string configName);

    [LoggerMessage(EventId = EventIds.ConfigurationByNameFound, Level = LogLevel.Debug, Message = "Config found: {Found}")]
    private partial void LogConfigurationByNameFound(bool found);

    [LoggerMessage(EventId = EventIds.ConfigurationVersionDetails, Level = LogLevel.Debug, Message = "Found config '{ConfigName}' with {VersionCount} versions, activeVersion: {ActiveVersion}")]
    private partial void LogConfigurationVersionDetails(string configName, int versionCount, string? activeVersion);

    [LoggerMessage(EventId = EventIds.ConfigurationFileDetails, Level = LogLevel.Debug, Message = "Found {FileCount} files, mainFile found: {MainFileFound}")]
    private partial void LogConfigurationFileDetails(int fileCount, bool mainFileFound);

    [LoggerMessage(EventId = EventIds.NodeConfigurationContentSetFromFallback, Level = LogLevel.Debug, Message = "Set configContent from fallback: {ConfigContent}")]
    private partial void LogNodeConfigurationContentSetFromFallback(string configContent);

    [LoggerMessage(EventId = EventIds.NoConfigurationFoundForNode, Level = LogLevel.Warning, Message = "No configuration found for node {NodeId}")]
    private partial void LogNoConfigurationFoundForNode(Guid nodeId);

    [LoggerMessage(EventId = EventIds.ReturningNodeConfiguration, Level = LogLevel.Debug, Message = "Returning Ok with content: '{ConfigContent}'")]
    private partial void LogReturningNodeConfiguration(string? configContent);
}

public static class NodeEndpointExtensions
{
    public static void MapNodeEndpoints(this IEndpointRouteBuilder app)
        => app.ServiceProvider.GetRequiredService<NodeEndpoints>().MapNodeEndpoints(app);
}
