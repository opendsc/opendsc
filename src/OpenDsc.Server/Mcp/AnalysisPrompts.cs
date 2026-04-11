// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;

using Microsoft.Extensions.AI;

using ModelContextProtocol.Server;

namespace OpenDsc.Server.Mcp;

[McpServerPromptType]
public sealed class AnalysisPrompts
{
    [McpServerPrompt(Name = "analyze_fleet_health"), Description("Generate a prompt to analyze the overall health and compliance posture of the managed node fleet.")]
    public static ChatMessage AnalyzeFleetHealth()
    {
        return new ChatMessage(
            ChatRole.User,
            """
            Analyze the health and compliance posture of the managed node fleet.

            Follow these steps:
            1. Call `get_compliance_summary` to get an overview of node statuses.
            2. If there are any non-compliant or error nodes, call `get_non_compliant_nodes` to list them.
            3. Call `get_stale_nodes` to check for nodes that are not checking in on schedule.
            4. Call `get_unassigned_nodes` to find any nodes without a configuration.
            5. Call `get_failed_reports` to review recent compliance failures.

            Summarize findings as a health report with:
            - Overall fleet status (healthy, degraded, critical)
            - Key metrics (total nodes, compliant %, non-compliant count, stale count)
            - Top issues requiring attention
            - Recommended actions
            """);
    }

    [McpServerPrompt(Name = "investigate_node"), Description("Generate a prompt to investigate a specific node's compliance issues.")]
    public static ChatMessage InvestigateNode(
        [Description("The FQDN of the node to investigate.")] string nodeFqdn)
    {
        return new ChatMessage(
            ChatRole.User,
            $"""
            Investigate the compliance status of node `{nodeFqdn}`.

            Follow these steps:
            1. Call `get_node_details` with FQDN `{nodeFqdn}` to get the node's current state.
            2. Call `get_node_reports` with FQDN `{nodeFqdn}` to review its recent compliance history.
            3. If the node has a configuration assigned, call `get_configuration_details` with the configuration name.

            Summarize findings with:
            - Current node status and whether it is stale
            - Recent compliance trend (improving, stable, degrading)
            - Any errors or non-compliant reports
            - Recommended next steps
            """);
    }
}
