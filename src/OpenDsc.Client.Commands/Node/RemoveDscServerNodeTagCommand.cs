// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Nodes;

namespace OpenDsc.Client.Commands.Node;

[Cmdlet("Remove", "DscServerNodeTag")]
public sealed class RemoveDscServerNodeTagCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid NodeId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public Guid ScopeValueId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<NodeHttpService>();
        var request = new RemoveNodeTagRequest
        {
            ScopeValueId = ScopeValueId,
        };

        service.RemoveNodeTagAsync(NodeId, request, CancellationToken.None).GetAwaiter().GetResult();
    }
}
