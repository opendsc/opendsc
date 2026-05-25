// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Nodes;

namespace OpenDsc.Client.Commands.Node;

[Cmdlet("Get", "DscServerNodeGetNodes")]
[OutputType(typeof(NodeSummary))]
public sealed class GetDscServerNodeGetNodesCommand : DscServerCommandBase
{
    [Parameter(Mandatory = false, Position = 0)]
    public NodeFilterRequest Filter { get; set; } = null!;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<NodeHttpService>();
        var result = service.GetNodesAsync(Filter, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
