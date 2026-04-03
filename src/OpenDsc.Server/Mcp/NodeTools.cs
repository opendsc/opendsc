// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.Text;

using Microsoft.EntityFrameworkCore;

using ModelContextProtocol.Server;

using OpenDsc.Server.Data;
using OpenDsc.Server.Entities;

namespace OpenDsc.Server.Mcp;

[McpServerToolType]
public sealed class NodeTools(ServerDbContext db)
{
    [McpServerTool(Name = "get_non_compliant_nodes"), Description("Get a list of nodes that are not in compliance. Returns nodes with NonCompliant or Error status.")]
    public async Task<string> GetNonCompliantNodes(CancellationToken cancellationToken)
    {
        var nodes = await db.Nodes
            .AsNoTracking()
            .Where(n => n.Status == NodeStatus.NonCompliant || n.Status == NodeStatus.Error)
            .OrderBy(n => n.Fqdn)
            .Select(n => new { n.Id, n.Fqdn, n.ConfigurationName, Status = n.Status.ToString(), n.LastCheckIn })
            .ToListAsync(cancellationToken);

        if (nodes.Count == 0)
        {
            return "All nodes are compliant. No non-compliant or error nodes found.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"## Non-Compliant Nodes ({nodes.Count})");
        sb.AppendLine();
        sb.AppendLine("| FQDN | Status | Configuration | Last Check-In |");
        sb.AppendLine("|------|--------|---------------|---------------|");

        foreach (var n in nodes)
        {
            sb.AppendLine($"| {n.Fqdn} | **{n.Status}** | {n.ConfigurationName ?? "—"} | {n.LastCheckIn?.ToString("u") ?? "never"} |");
        }

        sb.AppendLine();
        sb.AppendLine("**Hint:** Use `get_node_details` with a node's FQDN for full details, or `get_node_reports` for its compliance history.");

        return sb.ToString();
    }

    [McpServerTool(Name = "get_nodes_by_status"), Description("Get nodes filtered by compliance status. Valid statuses: Unknown, Compliant, NonCompliant, Error.")]
    public async Task<string> GetNodesByStatus(
        [Description("The compliance status to filter by (Unknown, Compliant, NonCompliant, Error).")] string status,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<NodeStatus>(status, ignoreCase: true, out var nodeStatus))
        {
            return $"Invalid status `{status}`. Valid values are: `Unknown`, `Compliant`, `NonCompliant`, `Error`.";
        }

        var nodes = await db.Nodes
            .AsNoTracking()
            .Where(n => n.Status == nodeStatus)
            .OrderBy(n => n.Fqdn)
            .Select(n => new { n.Id, n.Fqdn, n.ConfigurationName, n.LastCheckIn })
            .ToListAsync(cancellationToken);

        if (nodes.Count == 0)
        {
            return $"No nodes found with status **{nodeStatus}**.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"## Nodes with Status: {nodeStatus} ({nodes.Count})");
        sb.AppendLine();
        sb.AppendLine("| FQDN | Configuration | Last Check-In |");
        sb.AppendLine("|------|---------------|---------------|");

        foreach (var n in nodes)
        {
            sb.AppendLine($"| {n.Fqdn} | {n.ConfigurationName ?? "—"} | {n.LastCheckIn?.ToString("u") ?? "never"} |");
        }

        sb.AppendLine();
        sb.AppendLine("**Hint:** Use `get_node_details` with a node's FQDN for full details.");

        return sb.ToString();
    }

    [McpServerTool(Name = "get_node_details"), Description("Get detailed information about a specific node by its FQDN or ID. Includes compliance status, LCM status, configuration, staleness, and certificate info.")]
    public async Task<string> GetNodeDetails(
        [Description("The node's fully qualified domain name or GUID identifier.")] string nodeIdentifier,
        CancellationToken cancellationToken)
    {
        var settings = await db.ServerSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var staleness = settings?.StalenessMultiplier ?? 2.0;
        var now = DateTimeOffset.UtcNow;

        Node? node;
        if (Guid.TryParse(nodeIdentifier, out var nodeId))
        {
            node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == nodeId, cancellationToken);
        }
        else
        {
            node = await db.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Fqdn == nodeIdentifier, cancellationToken);
        }

        if (node is null)
        {
            return $"Node `{nodeIdentifier}` not found.";
        }

        var isStale = node.LastCheckIn.HasValue
            && node.ConfigurationModeInterval.HasValue
            && (now - node.LastCheckIn.Value) > node.ConfigurationModeInterval.Value * staleness;

        var sb = new StringBuilder();
        sb.AppendLine($"## Node: {node.Fqdn}");
        sb.AppendLine();
        sb.AppendLine("| Property | Value |");
        sb.AppendLine("|----------|-------|");
        sb.AppendLine($"| **ID** | `{node.Id}` |");
        sb.AppendLine($"| **Status** | **{node.Status}** |");
        sb.AppendLine($"| **LCM Status** | {node.LcmStatus} |");
        sb.AppendLine($"| **Configuration** | {node.ConfigurationName ?? "—"} |");
        sb.AppendLine($"| **Configuration Source** | {node.ConfigurationSource} |");
        sb.AppendLine($"| **Configuration Mode** | {node.ConfigurationMode?.ToString() ?? "—"} |");
        sb.AppendLine($"| **Configuration Interval** | {node.ConfigurationModeInterval?.ToString() ?? "—"} |");
        sb.AppendLine($"| **Report Compliance** | {node.ReportCompliance?.ToString() ?? "—"} |");
        sb.AppendLine($"| **Is Stale** | {isStale} |");
        sb.AppendLine($"| **Last Check-In** | {node.LastCheckIn?.ToString("u") ?? "never"} |");
        sb.AppendLine($"| **Registered** | {node.CreatedAt:u} |");
        sb.AppendLine($"| **Certificate Expires** | {node.CertificateNotAfter:u} |");
        sb.AppendLine();
        sb.AppendLine("**Hint:** Use `get_node_reports` to see this node's compliance report history.");

        return sb.ToString();
    }

    [McpServerTool(Name = "get_stale_nodes"), Description("Get nodes that have not checked in within the expected interval (stale nodes). A node is stale when its last check-in exceeds ConfigurationModeInterval multiplied by the staleness multiplier.")]
    public async Task<string> GetStaleNodes(CancellationToken cancellationToken)
    {
        var settings = await db.ServerSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var staleness = settings?.StalenessMultiplier ?? 2.0;
        var now = DateTimeOffset.UtcNow;

        var nodes = await db.Nodes.AsNoTracking().ToListAsync(cancellationToken);

        var staleNodes = nodes
            .Where(n => n.LastCheckIn.HasValue
                && n.ConfigurationModeInterval.HasValue
                && (now - n.LastCheckIn.Value) > n.ConfigurationModeInterval.Value * staleness)
            .OrderBy(n => n.LastCheckIn)
            .ToList();

        if (staleNodes.Count == 0)
        {
            return "No stale nodes found. All nodes are checking in on schedule.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"## Stale Nodes ({staleNodes.Count})");
        sb.AppendLine();
        sb.AppendLine("| FQDN | Status | Last Check-In |");
        sb.AppendLine("|------|--------|---------------|");

        foreach (var n in staleNodes)
        {
            sb.AppendLine($"| {n.Fqdn} | **{n.Status}** | {n.LastCheckIn?.ToString("u") ?? "never"} |");
        }

        sb.AppendLine();
        sb.AppendLine("**Hint:** Use `get_node_details` with a node's FQDN to investigate further.");

        return sb.ToString();
    }

    [McpServerTool(Name = "get_compliance_summary"), Description("Get an overall compliance summary showing the count of nodes in each status category. Use this as a starting point to understand the fleet's health.")]
    public async Task<string> GetComplianceSummary(CancellationToken cancellationToken)
    {
        var settings = await db.ServerSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var staleness = settings?.StalenessMultiplier ?? 2.0;
        var now = DateTimeOffset.UtcNow;

        var nodes = await db.Nodes.AsNoTracking().ToListAsync(cancellationToken);

        var statusGroups = nodes
            .GroupBy(n => n.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var staleCount = nodes.Count(n =>
            n.LastCheckIn.HasValue
            && n.ConfigurationModeInterval.HasValue
            && (now - n.LastCheckIn.Value) > n.ConfigurationModeInterval.Value * staleness);

        var sb = new StringBuilder();
        sb.AppendLine($"## Compliance Summary ({nodes.Count} total nodes)");
        sb.AppendLine();
        sb.AppendLine("| Status | Count |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Compliant | {statusGroups.GetValueOrDefault(NodeStatus.Compliant)} |");
        sb.AppendLine($"| Non-Compliant | {statusGroups.GetValueOrDefault(NodeStatus.NonCompliant)} |");
        sb.AppendLine($"| Error | {statusGroups.GetValueOrDefault(NodeStatus.Error)} |");
        sb.AppendLine($"| Unknown | {statusGroups.GetValueOrDefault(NodeStatus.Unknown)} |");
        sb.AppendLine($"| Stale | {staleCount} |");
        sb.AppendLine();
        sb.AppendLine("**Hint:** Use `get_non_compliant_nodes` or `get_stale_nodes` to drill into problem areas, or `list_configurations` to review configuration assignments.");

        return sb.ToString();
    }
}
