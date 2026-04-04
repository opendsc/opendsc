// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.Text;

using Microsoft.EntityFrameworkCore;

using ModelContextProtocol.Server;

using OpenDsc.Server.Authorization;
using OpenDsc.Server.Data;
using OpenDsc.Server.Services;

namespace OpenDsc.Server.Mcp;

[McpServerToolType]
public sealed class ConfigurationTools(
    ServerDbContext db,
    IUserContextService userContext,
    IResourceAuthorizationService authService)
{
    [McpServerTool(Name = "list_configurations"), Description("List all available DSC configurations with their assigned node counts and version counts.")]
    public async Task<string> ListConfigurations(CancellationToken cancellationToken)
    {
        var userId = userContext.GetCurrentUserId();
        if (userId == null)
        {
            return "No configurations found.";
        }

        var readableIds = await authService.GetReadableConfigurationIdsAsync(userId.Value);

        var configs = await db.Configurations
            .AsNoTracking()
            .Where(c => readableIds.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                NodeCount = c.NodeConfigurations.Count,
                VersionCount = c.Versions.Count,
                c.CreatedAt,
                c.UpdatedAt
            })
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        if (configs.Count == 0)
        {
            return "No configurations found.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"## Configurations ({configs.Count})");
        sb.AppendLine();
        sb.AppendLine("| Name | Nodes | Versions | Description |");
        sb.AppendLine("|------|-------|----------|-------------|");

        foreach (var c in configs)
        {
            sb.AppendLine($"| {c.Name} | {c.NodeCount} | {c.VersionCount} | {c.Description ?? "—"} |");
        }

        sb.AppendLine();
        sb.AppendLine("**Hint:** Use `get_configuration_details` with a configuration name for full details including assigned nodes and versions.");

        return sb.ToString();
    }

    [McpServerTool(Name = "get_configuration_details"), Description("Get detailed information about a specific configuration including its versions and assigned nodes.")]
    public async Task<string> GetConfigurationDetails(
        [Description("The configuration name or GUID identifier.")] string configIdentifier,
        CancellationToken cancellationToken)
    {
        IQueryable<Entities.Configuration> query = db.Configurations
            .AsNoTracking();

        if (Guid.TryParse(configIdentifier, out var configId))
        {
            query = query.Where(c => c.Id == configId);
        }
        else
        {
            query = query.Where(c => c.Name == configIdentifier);
        }

        var config = await query.FirstOrDefaultAsync(cancellationToken);

        if (config is null)
        {
            return $"Configuration `{configIdentifier}` not found.";
        }

        var userId = userContext.GetCurrentUserId();
        if (userId == null || !await authService.CanReadConfigurationAsync(userId.Value, config.Id))
        {
            return $"Configuration `{configIdentifier}` not found.";
        }

        var assignedNodes = await db.NodeConfigurations
            .AsNoTracking()
            .Where(nc => nc.ConfigurationId == config.Id)
            .Join(db.Nodes.AsNoTracking(), nc => nc.NodeId, n => n.Id, (nc, n) => new { n.Fqdn, n.Status, nc.ActiveVersion })
            .ToListAsync(cancellationToken);

        var versions = await db.ConfigurationVersions
            .AsNoTracking()
            .Where(v => v.ConfigurationId == config.Id)
            .OrderByDescending(v => v.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine($"## Configuration: {config.Name}");
        sb.AppendLine();
        sb.AppendLine("| Property | Value |");
        sb.AppendLine("|----------|-------|");
        sb.AppendLine($"| **ID** | `{config.Id}` |");
        sb.AppendLine($"| **Description** | {config.Description ?? "—"} |");
        sb.AppendLine($"| **Server-Managed Parameters** | {config.UseServerManagedParameters} |");
        sb.AppendLine($"| **Created** | {config.CreatedAt:u} |");
        sb.AppendLine($"| **Updated** | {config.UpdatedAt?.ToString("u") ?? "—"} |");
        sb.AppendLine();

        sb.AppendLine($"### Assigned Nodes ({assignedNodes.Count})");
        sb.AppendLine();
        if (assignedNodes.Count > 0)
        {
            sb.AppendLine("| FQDN | Status | Version |");
            sb.AppendLine("|------|--------|---------|");
            foreach (var n in assignedNodes)
            {
                sb.AppendLine($"| {n.Fqdn} | **{n.Status}** | {n.ActiveVersion ?? "—"} |");
            }
        }
        else
        {
            sb.AppendLine("No nodes assigned to this configuration.");
        }

        sb.AppendLine();
        sb.AppendLine($"### Recent Versions (up to 5)");
        sb.AppendLine();
        if (versions.Count > 0)
        {
            sb.AppendLine("| Version | Created |");
            sb.AppendLine("|---------|---------|");
            foreach (var v in versions)
            {
                sb.AppendLine($"| {v.Version} | {v.CreatedAt:u} |");
            }
        }
        else
        {
            sb.AppendLine("No versions published yet.");
        }

        sb.AppendLine();
        sb.AppendLine("**Hint:** Use `get_node_details` with a node's FQDN to see its full status.");

        return sb.ToString();
    }

    [McpServerTool(Name = "get_unassigned_nodes"), Description("Get nodes that do not have a configuration assigned. These nodes are not being managed.")]
    public async Task<string> GetUnassignedNodes(CancellationToken cancellationToken)
    {
        if (!userContext.HasPermission(Permissions.Nodes_Read))
        {
            return "Access denied. You need the `nodes.read` permission.";
        }

        var nodes = await db.Nodes
            .AsNoTracking()
            .Where(n => n.ConfigurationName == null)
            .OrderBy(n => n.Fqdn)
            .Select(n => new { n.Id, n.Fqdn, n.Status, n.LastCheckIn })
            .ToListAsync(cancellationToken);

        if (nodes.Count == 0)
        {
            return "All nodes have a configuration assigned.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"## Unassigned Nodes ({nodes.Count})");
        sb.AppendLine();
        sb.AppendLine("| FQDN | Status | Last Check-In |");
        sb.AppendLine("|------|--------|---------------|");

        foreach (var n in nodes)
        {
            sb.AppendLine($"| {n.Fqdn} | {n.Status} | {n.LastCheckIn?.ToString("u") ?? "never"} |");
        }

        sb.AppendLine();
        sb.AppendLine("**Hint:** Use `list_configurations` to see available configurations that can be assigned to these nodes.");

        return sb.ToString();
    }
}
