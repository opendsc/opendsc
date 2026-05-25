// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Node;

[Cmdlet("Remove", "DscServerNode")]
public sealed class RemoveDscServerNodeCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid NodeId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<NodeHttpService>();
        service.DeleteNodeAsync(NodeId, CancellationToken.None).GetAwaiter().GetResult();
    }
}
