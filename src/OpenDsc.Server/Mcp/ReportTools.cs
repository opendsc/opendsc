// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.Text;

using Microsoft.EntityFrameworkCore;

using ModelContextProtocol.Server;

using OpenDsc.Server.Data;

namespace OpenDsc.Server.Mcp;

[McpServerToolType]
public sealed class ReportTools(ServerDbContext db)
{
    [McpServerTool(Name = "get_recent_reports"), Description("Get the most recent compliance reports across all nodes. Use this to see recent activity.")]
    public async Task<string> GetRecentReports(
        [Description("Maximum number of reports to return (default: 10, max: 50).")] int? count,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(count ?? 10, 1, 50);

        var reports = await db.Reports
            .AsNoTracking()
            .Include(r => r.Node)
            .OrderByDescending(r => r.Timestamp)
            .Take(take)
            .Select(r => new { r.Id, r.NodeId, NodeFqdn = r.Node!.Fqdn, r.Timestamp, Operation = r.Operation.ToString(), r.InDesiredState, r.HadErrors })
            .ToListAsync(cancellationToken);

        if (reports.Count == 0)
        {
            return "No compliance reports found.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"## Recent Reports ({reports.Count})");
        sb.AppendLine();
        sb.AppendLine("| Timestamp | Node | Operation | Compliant | Errors |");
        sb.AppendLine("|-----------|------|-----------|-----------|--------|");

        foreach (var r in reports)
        {
            sb.AppendLine($"| {r.Timestamp:u} | {r.NodeFqdn} | {r.Operation} | {(r.InDesiredState ? "Yes" : "**No**")} | {(r.HadErrors ? "**Yes**" : "No")} |");
        }

        sb.AppendLine();
        sb.AppendLine("**Hint:** Use `get_node_reports` with a node's FQDN to see its full history, or `get_failed_reports` to focus on problems.");

        return sb.ToString();
    }

    [McpServerTool(Name = "get_node_reports"), Description("Get compliance reports for a specific node. Use this to review a node's compliance history.")]
    public async Task<string> GetNodeReports(
        [Description("The node's fully qualified domain name or GUID identifier.")] string nodeIdentifier,
        [Description("Maximum number of reports to return (default: 10, max: 50).")] int? count,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(count ?? 10, 1, 50);

        IQueryable<Entities.Node> query = db.Nodes.AsNoTracking();

        if (Guid.TryParse(nodeIdentifier, out var nodeId))
        {
            query = query.Where(n => n.Id == nodeId);
        }
        else
        {
            query = query.Where(n => n.Fqdn == nodeIdentifier);
        }

        var node = await query.FirstOrDefaultAsync(cancellationToken);
        if (node is null)
        {
            return $"Node `{nodeIdentifier}` not found.";
        }

        var reports = await db.Reports
            .AsNoTracking()
            .Where(r => r.NodeId == node.Id)
            .OrderByDescending(r => r.Timestamp)
            .Take(take)
            .Select(r => new { r.Id, r.Timestamp, Operation = r.Operation.ToString(), r.InDesiredState, r.HadErrors })
            .ToListAsync(cancellationToken);

        if (reports.Count == 0)
        {
            return $"No compliance reports found for node `{node.Fqdn}`.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"## Reports for {node.Fqdn} (showing {reports.Count})");
        sb.AppendLine();
        sb.AppendLine("| Timestamp | Operation | Compliant | Errors |");
        sb.AppendLine("|-----------|-----------|-----------|--------|");

        foreach (var r in reports)
        {
            sb.AppendLine($"| {r.Timestamp:u} | {r.Operation} | {(r.InDesiredState ? "Yes" : "**No**")} | {(r.HadErrors ? "**Yes**" : "No")} |");
        }

        sb.AppendLine();
        sb.AppendLine("**Hint:** Use `get_node_details` with this node's FQDN for its current status and configuration info.");

        return sb.ToString();
    }

    [McpServerTool(Name = "get_failed_reports"), Description("Get recent reports that had errors or were not in desired state. Use this to find compliance failures.")]
    public async Task<string> GetFailedReports(
        [Description("Maximum number of reports to return (default: 10, max: 50).")] int? count,
        CancellationToken cancellationToken)
    {
        var take = Math.Clamp(count ?? 10, 1, 50);

        var reports = await db.Reports
            .AsNoTracking()
            .Include(r => r.Node)
            .Where(r => !r.InDesiredState || r.HadErrors)
            .OrderByDescending(r => r.Timestamp)
            .Take(take)
            .Select(r => new { r.Id, NodeFqdn = r.Node!.Fqdn, r.Timestamp, Operation = r.Operation.ToString(), r.InDesiredState, r.HadErrors })
            .ToListAsync(cancellationToken);

        if (reports.Count == 0)
        {
            return "No failed or non-compliant reports found.";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"## Failed/Non-Compliant Reports ({reports.Count})");
        sb.AppendLine();
        sb.AppendLine("| Timestamp | Node | Operation | Compliant | Errors |");
        sb.AppendLine("|-----------|------|-----------|-----------|--------|");

        foreach (var r in reports)
        {
            sb.AppendLine($"| {r.Timestamp:u} | {r.NodeFqdn} | {r.Operation} | {(r.InDesiredState ? "Yes" : "**No**")} | {(r.HadErrors ? "**Yes**" : "No")} |");
        }

        sb.AppendLine();
        sb.AppendLine("**Hint:** Use `get_node_details` with a node's FQDN to investigate its current state.");

        return sb.ToString();
    }
}
