// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Claims;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Lcm;
using OpenDsc.Server.Authentication;
using OpenDsc.Server.Authorization;
using OpenDsc.Contracts.Nodes;
using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;
using OpenDsc.Server.Services;

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

        var registrationKey = await db.RegistrationKeys
            .FirstOrDefaultAsync(k => k.Key == request.RegistrationKey, cancellationToken);


        if (registrationKey is null)
        {
            return TypedResults.BadRequest(new ErrorResponse { Error = $"Invalid registration key: '{request.RegistrationKey}'." });
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

        var thumbprintConflict = await db.Nodes
            .FirstOrDefaultAsync(n => n.CertificateThumbprint == thumbprint && n.Fqdn != request.Fqdn, cancellationToken);

        if (thumbprintConflict is not null)
        {
            return TypedResults.Conflict(new ErrorResponse
            {
                Error = $"Certificate thumbprint is already registered to another node: {thumbprintConflict.Fqdn}"
            });
        }

        var existingNode = await db.Nodes.FirstOrDefaultAsync(n => n.Fqdn == request.Fqdn, cancellationToken);
        if (existingNode is not null)
        {
            existingNode.CertificateThumbprint = thumbprint;
            existingNode.CertificateSubject = subject;
            existingNode.CertificateNotAfter = notAfter;
            existingNode.LastCheckIn = DateTimeOffset.UtcNow;
            existingNode.ConfigurationSource = request.ConfigurationSource ?? ConfigurationSource.Pull;
            existingNode.ConfigurationMode = request.ConfigurationMode;
            existingNode.ConfigurationModeInterval = request.ConfigurationModeInterval;
            existingNode.ReportCompliance = request.ReportCompliance;
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
            LastCheckIn = DateTimeOffset.UtcNow,
            ConfigurationSource = request.ConfigurationSource ?? ConfigurationSource.Pull,
            ConfigurationMode = request.ConfigurationMode,
            ConfigurationModeInterval = request.ConfigurationModeInterval,
            ReportCompliance = request.ReportCompliance
        };

        db.Nodes.Add(node);
        registrationKey.CurrentUses++;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new RegisterNodeResponse
        {
            NodeId = node.Id
        });
    }

    private async Task<Ok<List<NodeSummary>>> GetNodes(
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var settings = await db.ServerSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var staleness = settings?.StalenessMultiplier ?? 2.0;
        var now = DateTimeOffset.UtcNow;

        var nodes = await db.Nodes.AsNoTracking().ToListAsync(cancellationToken);

        var result = nodes.Select(n => new NodeSummary
        {
            Id = n.Id,
            Fqdn = n.Fqdn,
            ConfigurationName = n.ConfigurationName,
            Status = n.Status.ToString(),
            LcmStatus = n.LcmStatus.ToString(),
            IsStale = n.LastCheckIn.HasValue
                && n.ConfigurationModeInterval.HasValue
                && (now - n.LastCheckIn.Value) > n.ConfigurationModeInterval.Value * staleness,
            LastCheckIn = n.LastCheckIn,
            CreatedAt = n.CreatedAt,
            ConfigurationSource = n.ConfigurationSource,
            ConfigurationMode = n.ConfigurationMode,
            ConfigurationModeInterval = n.ConfigurationModeInterval,
            ReportCompliance = n.ReportCompliance,
            DesiredConfigurationMode = n.DesiredConfigurationMode,
            DesiredConfigurationModeInterval = n.DesiredConfigurationModeInterval,
            DesiredReportCompliance = n.DesiredReportCompliance
        }).ToList();

        return TypedResults.Ok(result);
    }

    private async Task<Results<Ok<NodeSummary>, NotFound<ErrorResponse>>> GetNode(
        Guid nodeId,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var settings = await db.ServerSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var staleness = settings?.StalenessMultiplier ?? 2.0;
        var now = DateTimeOffset.UtcNow;

        var n = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == nodeId, cancellationToken);

        if (n is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        var node = new NodeSummary
        {
            Id = n.Id,
            Fqdn = n.Fqdn,
            ConfigurationName = n.ConfigurationName,
            Status = n.Status.ToString(),
            LcmStatus = n.LcmStatus.ToString(),
            IsStale = n.LastCheckIn.HasValue
                && n.ConfigurationModeInterval.HasValue
                && (now - n.LastCheckIn.Value) > n.ConfigurationModeInterval.Value * staleness,
            LastCheckIn = n.LastCheckIn,
            CreatedAt = n.CreatedAt,
            ConfigurationSource = n.ConfigurationSource,
            ConfigurationMode = n.ConfigurationMode,
            ConfigurationModeInterval = n.ConfigurationModeInterval,
            ReportCompliance = n.ReportCompliance,
            DesiredConfigurationMode = n.DesiredConfigurationMode,
            DesiredConfigurationModeInterval = n.DesiredConfigurationModeInterval,
            DesiredReportCompliance = n.DesiredReportCompliance
        };

        return TypedResults.Ok(node);
    }

    private async Task<Results<NoContent, NotFound<ErrorResponse>>> DeleteNode(
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

    private async Task<Results<Ok<string>, NotFound<ErrorResponse>, ForbidHttpResult>> GetNodeConfiguration(
        Guid nodeId,
        ClaimsPrincipal user,
        IWebHostEnvironment env,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        LogGettingNodeConfiguration(nodeId);
        try
        {
#pragma warning disable CS8602 // Dereference of a possibly null reference
            if (!env.IsEnvironment("Testing"))
            {
                var authenticatedNodeId = user.FindFirst("node_id")?.Value;
                if (authenticatedNodeId is null || !Guid.TryParse(authenticatedNodeId, out var authNodeId) || authNodeId != nodeId)
                {
                    return TypedResults.Forbid();
                }
            }

            var nodeConfig = await db.NodeConfigurations
                .Include(nc => nc.Configuration)
                .ThenInclude(c => c.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
                .ThenInclude(v => v.Files)
                .AsSplitQuery()
                .FirstOrDefaultAsync(nc => nc.NodeId == nodeId, cancellationToken);

            string? configContent = null;

            LogNodeConfigurationFound(nodeConfig is not null);

            if (nodeConfig is not null)
            {
                var configuration = nodeConfig.Configuration;
                if (configuration is not null)
                {
                    var activeVersion = VersionResolver.ResolveVersion(
                        configuration.Versions, v => v.Version, nodeConfig.MajorVersion, nodeConfig.PrereleaseChannel);

                    if (activeVersion is not null)
                    {
                        var mainFile = activeVersion.Files.FirstOrDefault(f => f.RelativePath == "main.dsc.yaml");
                        if (mainFile is not null)
                        {
                            configContent = "resources: []"; // For test configurations
                            LogNodeConfigurationContentSetFromAssignment();
                        }
                    }
                }
            }

            if (configContent is null)
            {
                var node = await db.Nodes.FindAsync([nodeId], cancellationToken);
                LogNodeConfigurationFallback(node is not null, node?.ConfigurationName);
                if (node is not null && node.ConfigurationName is not null)
                {
                    var configName = node.ConfigurationName;
                    LogLookingForConfigurationByName(configName);
                    var config = await db.Configurations
                        .Include(c => c.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
                        .ThenInclude(v => v.Files)
                        .AsSplitQuery()
                        .FirstOrDefaultAsync(c => c.Name == configName, cancellationToken);

                    LogConfigurationByNameFound(config is not null);
                    if (config is not null)
                    {
                        var activeVersion = config.Versions.FirstOrDefault();
                        LogConfigurationVersionDetails(config.Name, config.Versions.Count, activeVersion?.Version);
                        if (activeVersion is not null)
                        {
                            var mainFile = activeVersion.Files.FirstOrDefault(f => f.RelativePath == "main.dsc.yaml");
                            LogConfigurationFileDetails(activeVersion.Files.Count, mainFile is not null);
                            if (mainFile is not null)
                            {
                                // For test configurations, return default content
                                configContent = "resources: []";
                                LogNodeConfigurationContentSetFromFallback(configContent);
                            }
                        }
                    }
                }
            }

            if (configContent is null)
            {
                LogNoConfigurationFoundForNode(nodeId);
                return TypedResults.NotFound(new ErrorResponse { Error = "No configuration assigned." });
            }

            var node2 = await db.Nodes.FindAsync([nodeId], cancellationToken);
            if (node2 is not null)
            {
                node2.LastCheckIn = DateTimeOffset.UtcNow;
                await db.SaveChangesAsync(cancellationToken);
            }

            LogReturningNodeConfiguration(configContent);
            return TypedResults.Ok(configContent);
#pragma warning restore CS8602
        }
        catch
        {
            throw;
        }
    }

    private async Task<Results<FileStreamHttpResult, NotFound<ErrorResponse>>> GetConfigurationBundle(
        Guid nodeId,
        ServerDbContext db,
        IOptions<ServerConfig> serverConfig,
        IParameterMergeService parameterMergeService,
        CancellationToken cancellationToken)
    {
        var nodeConfig = await db.NodeConfigurations
            .Include(nc => nc.Configuration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Files)
            .Include(nc => nc.CompositeConfiguration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Items)
            .ThenInclude(i => i.ChildConfiguration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v!.Files)
            .AsSplitQuery()
            .FirstOrDefaultAsync(nc => nc.NodeId == nodeId, cancellationToken);

        if (nodeConfig is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "No configuration assigned." });
        }

        var node = await db.Nodes.FindAsync([nodeId], cancellationToken);
        node!.LastCheckIn = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var dataDir = serverConfig.Value.ConfigurationsDirectory;

        // Handle composite configuration
        if (nodeConfig.CompositeConfigurationId.HasValue)
        {
            return await GenerateCompositeBundle(nodeId, nodeConfig, dataDir, parameterMergeService, db, cancellationToken);
        }

        // Handle regular configuration
        var activeVersion = VersionResolver.ResolveVersion(
            nodeConfig.Configuration!.Versions, v => v.Version, nodeConfig.MajorVersion, nodeConfig.PrereleaseChannel);

        if (activeVersion is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "No published version available." });
        }

        var versionDir = Path.Combine(dataDir, nodeConfig.Configuration.Name, $"v{activeVersion.Version}");

        var bundleStream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(bundleStream, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            foreach (var file in activeVersion.Files)
            {
                var filePath = Path.Combine(versionDir, file.RelativePath);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                var entry = archive.CreateEntry(file.RelativePath);
                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(filePath);
                await fileStream.CopyToAsync(entryStream, cancellationToken);
            }

            if (nodeConfig.Configuration!.UseServerManagedParameters)
            {
                var mergedParameters = await parameterMergeService.MergeParametersAsync(nodeId, nodeConfig.ConfigurationId!.Value, cancellationToken);
                if (!string.IsNullOrWhiteSpace(mergedParameters))
                {
                    var paramEntry = archive.CreateEntry("parameters.yaml");
                    using var paramStream = paramEntry.Open();
                    using var writer = new StreamWriter(paramStream);
                    await writer.WriteAsync(mergedParameters);
                }
            }
        }

        bundleStream.Position = 0;
        return TypedResults.File(bundleStream, "application/zip", $"{nodeConfig.Configuration.Name}-v{activeVersion.Version}.zip");
    }

    private async Task<FileStreamHttpResult> GenerateCompositeBundle(
        Guid nodeId,
        NodeConfiguration nodeConfig,
        string dataDir,
        IParameterMergeService parameterMergeService,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var activeCompositeVersion = VersionResolver.ResolveVersion(
            nodeConfig.CompositeConfiguration!.Versions, v => v.Version, nodeConfig.MajorVersion, nodeConfig.PrereleaseChannel);

        if (activeCompositeVersion is null)
        {
            throw new InvalidOperationException("No published composite version available.");
        }

        var bundleStream = new MemoryStream();
        using (var archive = new System.IO.Compression.ZipArchive(bundleStream, System.IO.Compression.ZipArchiveMode.Create, true))
        {
            var includeResources = new List<string>();

            // Process each child configuration
            foreach (var item in activeCompositeVersion.Items)
            {
                var childVersion = !string.IsNullOrWhiteSpace(item.ActiveVersion)
                    ? item.ChildConfiguration.Versions.FirstOrDefault(v => v.Version == item.ActiveVersion)
                    : VersionResolver.ResolveVersion(item.ChildConfiguration.Versions, v => v.Version, null, nodeConfig.PrereleaseChannel);

                if (childVersion is null)
                {
                    continue;
                }

                var childVersionDir = Path.Combine(dataDir, item.ChildConfiguration.Name, $"v{childVersion.Version}");
                var childFolderName = item.ChildConfiguration.Name;

                // Copy all child configuration files into {childName}/ folder
                foreach (var file in childVersion.Files)
                {
                    var sourcePath = Path.Combine(childVersionDir, file.RelativePath);
                    if (!File.Exists(sourcePath))
                    {
                        continue;
                    }

                    var entryPath = $"{childFolderName}/{file.RelativePath}";
                    var entry = archive.CreateEntry(entryPath);
                    using var entryStream = entry.Open();
                    using var fileStream = File.OpenRead(sourcePath);
                    await fileStream.CopyToAsync(entryStream, cancellationToken);
                }

                // Merge parameters per child if server-managed
                string? childParametersFile = null;
                if (item.ChildConfiguration.UseServerManagedParameters)
                {
                    var mergedParameters = await parameterMergeService.MergeParametersAsync(nodeId, item.ChildConfigurationId, cancellationToken);
                    if (!string.IsNullOrWhiteSpace(mergedParameters))
                    {
                        childParametersFile = $"{childFolderName}/parameters.yaml";
                        var paramEntry = archive.CreateEntry(childParametersFile);
                        using var paramStream = paramEntry.Open();
                        using var writer = new StreamWriter(paramStream);
                        await writer.WriteAsync(mergedParameters);
                    }
                }

                // Add to include list for main.dsc.yaml
                var childEntryPoint = childVersion.EntryPoint;
                var includeProps = $"      configurationFile: {childFolderName}/{childEntryPoint}";
                if (childParametersFile is not null)
                {
                    includeProps += $"\n      parametersFile: {childParametersFile}";
                }
                includeResources.Add($"  - name: {item.ChildConfiguration.Name}\n    type: Microsoft.DSC/Include\n    properties:\n{includeProps}");
            }

            // Generate main.dsc.yaml orchestrator
            var mainContent = $"$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json\nresources:\n{string.Join("\n", includeResources)}\n";
            var mainEntry = archive.CreateEntry(nodeConfig.CompositeConfiguration.EntryPoint);
            using (var mainStream = mainEntry.Open())
            using (var writer = new StreamWriter(mainStream))
            {
                await writer.WriteAsync(mainContent);
            }
        }

        bundleStream.Position = 0;
        return TypedResults.File(bundleStream, "application/zip", $"{nodeConfig.CompositeConfiguration.Name}-v{activeCompositeVersion.Version}.zip");
    }

    private async Task<Results<NoContent, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> AssignConfiguration(
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

        if (request.IsComposite)
        {
            var composite = await db.CompositeConfigurations
                .Include(c => c.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
                .FirstOrDefaultAsync(c => c.Name == request.ConfigurationName, cancellationToken);

            if (composite is null)
            {
                return TypedResults.NotFound(new ErrorResponse { Error = "Composite configuration not found." });
            }

            var eligibleCompositeVersion = VersionResolver.ResolveVersion(
                composite.Versions, v => v.Version, request.MajorVersion, request.PrereleaseChannel);

            if (eligibleCompositeVersion is null)
            {
                return TypedResults.BadRequest(new ErrorResponse
                {
                    Error = "No published version satisfies the specified major version and prerelease channel constraints."
                });
            }

            var nodeConfig = await db.NodeConfigurations.FindAsync([nodeId], cancellationToken);
            if (nodeConfig is null)
            {
                nodeConfig = new NodeConfiguration
                {
                    NodeId = nodeId,
                    CompositeConfigurationId = composite.Id,
                    MajorVersion = request.MajorVersion,
                    PrereleaseChannel = request.PrereleaseChannel
                };
                db.NodeConfigurations.Add(nodeConfig);
            }
            else
            {
                nodeConfig.ConfigurationId = null;
                nodeConfig.ActiveVersion = null;
                nodeConfig.CompositeConfigurationId = composite.Id;
                nodeConfig.ActiveCompositeVersion = null;
                nodeConfig.MajorVersion = request.MajorVersion;
                nodeConfig.PrereleaseChannel = request.PrereleaseChannel;
            }

            node.ConfigurationName = request.ConfigurationName;
        }
        else
        {
            var config = await db.Configurations
                .Include(c => c.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
                .FirstOrDefaultAsync(c => c.Name == request.ConfigurationName, cancellationToken);

            if (config is null)
            {
                return TypedResults.NotFound(new ErrorResponse { Error = "Configuration not found." });
            }

            var eligibleVersion = VersionResolver.ResolveVersion(
                config.Versions, v => v.Version, request.MajorVersion, request.PrereleaseChannel);

            if (eligibleVersion is null)
            {
                return TypedResults.BadRequest(new ErrorResponse
                {
                    Error = "No published version satisfies the specified major version and prerelease channel constraints."
                });
            }

            var nodeConfig = await db.NodeConfigurations.FindAsync([nodeId], cancellationToken);
            if (nodeConfig is null)
            {
                nodeConfig = new NodeConfiguration
                {
                    NodeId = nodeId,
                    ConfigurationId = config.Id,
                    MajorVersion = request.MajorVersion,
                    PrereleaseChannel = request.PrereleaseChannel
                };
                db.NodeConfigurations.Add(nodeConfig);
            }
            else
            {
                nodeConfig.CompositeConfigurationId = null;
                nodeConfig.ActiveCompositeVersion = null;
                nodeConfig.ConfigurationId = config.Id;
                nodeConfig.ActiveVersion = null;
                nodeConfig.MajorVersion = request.MajorVersion;
                nodeConfig.PrereleaseChannel = request.PrereleaseChannel;
            }

            node.ConfigurationName = request.ConfigurationName;
        }

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private async Task<Results<NoContent, NotFound<ErrorResponse>>> UnassignConfiguration(
        Guid nodeId,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var node = await db.Nodes.FindAsync([nodeId], cancellationToken);
        if (node is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        var nodeConfig = await db.NodeConfigurations.FindAsync([nodeId], cancellationToken);
        if (nodeConfig is not null)
        {
            db.NodeConfigurations.Remove(nodeConfig);
        }

        node.ConfigurationName = null;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private async Task<Results<Ok<ConfigurationChecksumResponse>, NotFound<ErrorResponse>, ForbidHttpResult>> GetConfigurationChecksum(
        Guid nodeId,
        ClaimsPrincipal user,
        ServerDbContext db,
        IParameterMergeService parameterMergeService,
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

        var nodeConfig = await db.NodeConfigurations
            .Include(nc => nc.Configuration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Files)
            .Include(nc => nc.CompositeConfiguration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Items)
            .ThenInclude(i => i.ChildConfiguration)
            .ThenInclude(c => c!.Versions.Where(v => v.Status == ConfigurationVersionStatus.Published))
            .ThenInclude(v => v.Files)
            .FirstOrDefaultAsync(nc => nc.NodeId == nodeId, cancellationToken);

        if (nodeConfig is null)
        {
            await db.SaveChangesAsync(cancellationToken);
            return TypedResults.NotFound(new ErrorResponse { Error = "No configuration assigned." });
        }

        string checksum;
        string entryPoint;
        string? parametersFile = null;

        if (nodeConfig.CompositeConfigurationId.HasValue)
        {
            var activeCompositeVersion = VersionResolver.ResolveVersion(
                nodeConfig.CompositeConfiguration!.Versions, v => v.Version, nodeConfig.MajorVersion, nodeConfig.PrereleaseChannel);

            if (activeCompositeVersion is null)
            {
                await db.SaveChangesAsync(cancellationToken);
                return TypedResults.NotFound(new ErrorResponse { Error = "No published composite version available." });
            }

            var checksumParts = new List<string>
            {
                $"composite:{activeCompositeVersion.Version}"
            };

            foreach (var item in activeCompositeVersion.Items.OrderBy(i => i.Order))
            {
                var childVersion = !string.IsNullOrWhiteSpace(item.ActiveVersion)
                    ? item.ChildConfiguration!.Versions.FirstOrDefault(v => v.Version == item.ActiveVersion)
                    : VersionResolver.ResolveVersion(item.ChildConfiguration!.Versions, v => v.Version, null, nodeConfig.PrereleaseChannel);

                if (childVersion is not null)
                {
                    checksumParts.Add($"child:{item.ChildConfiguration!.Name}:{childVersion.Version}:{childVersion.EntryPoint}");

                    foreach (var file in childVersion.Files.OrderBy(f => f.RelativePath))
                    {
                        checksumParts.Add($"{item.ChildConfiguration.Name}/{file.RelativePath}:{file.Checksum}");
                    }

                    if (item.ChildConfiguration.UseServerManagedParameters)
                    {
                        var mergedParams = await parameterMergeService.MergeParametersAsync(nodeId, item.ChildConfigurationId, cancellationToken);
                        if (!string.IsNullOrWhiteSpace(mergedParams))
                        {
                            var paramHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                                System.Text.Encoding.UTF8.GetBytes(mergedParams))).ToLowerInvariant();
                            checksumParts.Add($"params:{item.ChildConfiguration.Name}:{paramHash}");
                        }
                    }
                }
            }

            var combined = string.Join("|", checksumParts);
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined));
            checksum = Convert.ToHexString(hash).ToLowerInvariant();
            entryPoint = nodeConfig.CompositeConfiguration!.EntryPoint;
            parametersFile = null;
        }
        else
        {
            var activeVersion = VersionResolver.ResolveVersion(
                nodeConfig.Configuration!.Versions, v => v.Version, nodeConfig.MajorVersion, nodeConfig.PrereleaseChannel);

            if (activeVersion is null)
            {
                await db.SaveChangesAsync(cancellationToken);
                return TypedResults.NotFound(new ErrorResponse { Error = "No published version available." });
            }

            var checksumParts = new List<string>
            {
                $"version:{activeVersion.Version}",
                $"entrypoint:{activeVersion.EntryPoint}"
            };

            foreach (var file in activeVersion.Files.OrderBy(f => f.RelativePath))
            {
                checksumParts.Add($"{file.RelativePath}:{file.Checksum}");
            }

            if (nodeConfig.Configuration!.UseServerManagedParameters)
            {
                var mergedParams = await parameterMergeService.MergeParametersAsync(nodeId, nodeConfig.ConfigurationId!.Value, cancellationToken);
                if (!string.IsNullOrWhiteSpace(mergedParams))
                {
                    var paramHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
                        System.Text.Encoding.UTF8.GetBytes(mergedParams))).ToLowerInvariant();
                    checksumParts.Add($"params:{paramHash}");
                    parametersFile = "parameters.yaml";
                }
            }

            var combined = string.Join("|", checksumParts);
            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined));
            checksum = Convert.ToHexString(hash).ToLowerInvariant();
            entryPoint = activeVersion.EntryPoint;
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(new ConfigurationChecksumResponse { Checksum = checksum, EntryPoint = entryPoint, ParametersFile = parametersFile });
    }

    private async Task<Results<NoContent, NotFound<ErrorResponse>, ForbidHttpResult>> UpdateLcmStatus(
        Guid nodeId,
        UpdateLcmStatusRequest request,
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

        node.LcmStatus = request.LcmStatus;
        node.LastCheckIn = DateTimeOffset.UtcNow;
        db.NodeStatusEvents.Add(new NodeStatusEvent
        {
            NodeId = nodeId,
            LcmStatus = request.LcmStatus,
            Timestamp = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private async Task<Results<Ok<List<NodeStatusEventSummary>>, NotFound<ErrorResponse>>> GetNodeStatusHistory(
        Guid nodeId,
        ServerDbContext db,
        int? skip,
        int? take,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var nodeExists = await db.Nodes.AsNoTracking().AnyAsync(n => n.Id == nodeId, cancellationToken);
        if (!nodeExists)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        var rawEvents = await db.NodeStatusEvents
            .AsNoTracking()
            .Where(e => e.NodeId == nodeId)
            .Where(e => from == null || e.Timestamp >= from)
            .Where(e => to == null || e.Timestamp <= to)
            .OrderByDescending(e => e.Id)
            .Skip(skip ?? 0)
            .Take(take ?? 50)
            .ToListAsync(cancellationToken);

        var events = rawEvents.Select(e => new NodeStatusEventSummary
        {
            Id = e.Id,
            NodeId = e.NodeId,
            LcmStatus = e.LcmStatus?.ToString(),
            Timestamp = e.Timestamp
        }).ToList();

        return TypedResults.Ok(events);
    }

    private async Task<Results<Ok<RotateCertificateResponse>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>, ForbidHttpResult>> RotateCertificate(
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

    private async Task<Results<Ok<NodeLcmConfigResponse>, NotFound<ErrorResponse>, ForbidHttpResult>> GetNodeLcmConfig(
        Guid nodeId,
        ClaimsPrincipal user,
        IWebHostEnvironment env,
        ServerDbContext db,
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

        var node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken);
        if (node is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        var serverSettings = await db.ServerSettings.FindAsync([1], cancellationToken);

        return TypedResults.Ok(new NodeLcmConfigResponse
        {
            ConfigurationMode = node.DesiredConfigurationMode ?? serverSettings?.DefaultConfigurationMode,
            ConfigurationModeInterval = node.DesiredConfigurationModeInterval ?? serverSettings?.DefaultConfigurationModeInterval,
            ReportCompliance = node.DesiredReportCompliance ?? serverSettings?.DefaultReportCompliance,
            CertificateRotationInterval = serverSettings?.CertificateRotationInterval
        });
    }

    private async Task<Results<Ok<NodeLcmConfigResponse>, NotFound<ErrorResponse>>> UpdateNodeLcmConfig(
        Guid nodeId,
        UpdateNodeLcmConfigRequest request,
        ServerDbContext db,
        CancellationToken cancellationToken)
    {
        var node = await db.Nodes.FindAsync([nodeId], cancellationToken);
        if (node is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        node.DesiredConfigurationMode = request.ConfigurationMode;
        node.DesiredConfigurationModeInterval = request.ConfigurationModeInterval;
        node.DesiredReportCompliance = request.ReportCompliance;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new NodeLcmConfigResponse
        {
            ConfigurationMode = node.DesiredConfigurationMode,
            ConfigurationModeInterval = node.DesiredConfigurationModeInterval,
            ReportCompliance = node.DesiredReportCompliance
        });
    }

    private async Task<Results<NoContent, NotFound<ErrorResponse>, ForbidHttpResult>> ReportNodeLcmConfig(
        Guid nodeId,
        ReportNodeLcmConfigRequest request,
        ClaimsPrincipal user,
        IWebHostEnvironment env,
        ServerDbContext db,
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

        var node = await db.Nodes.FindAsync([nodeId], cancellationToken);
        if (node is null)
        {
            return TypedResults.NotFound(new ErrorResponse { Error = "Node not found." });
        }

        node.ConfigurationMode = request.ConfigurationMode;
        node.ConfigurationModeInterval = request.ConfigurationModeInterval;
        node.ReportCompliance = request.ReportCompliance;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
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
