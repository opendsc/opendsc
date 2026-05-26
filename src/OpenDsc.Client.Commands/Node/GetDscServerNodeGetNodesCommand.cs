// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Nodes;

namespace OpenDsc.Client.Commands.Node;

[Cmdlet(VerbsCommon.Get, "DscServerNodeGetNodes")]
[OutputType(typeof(NodeSummary))]
public sealed class GetDscServerNodeGetNodesCommand : DscServerCommandBase
{
    [Parameter]
    public string? FqdnContains { get; set; }

    [Parameter]
    public string? ConfigurationContains { get; set; }

    [Parameter]
    public NodeStatus? Status { get; set; }

    [Parameter]
    public LcmStatus? LcmStatus { get; set; }

    [Parameter]
    public int? Limit { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<NodeHttpService>();
        NodeFilterRequest? filter = null;
        if (FqdnContains is not null || ConfigurationContains is not null || Status.HasValue || LcmStatus.HasValue || Limit.HasValue)
        {
            filter = new NodeFilterRequest
            {
                FqdnContains = FqdnContains,
                ConfigurationContains = ConfigurationContains,
                Status = Status,
                LcmStatus = LcmStatus,
                Limit = Limit,
            };
        }

        var result = service.GetNodesAsync(filter, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
