// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Nodes;

namespace OpenDsc.Client.Commands.Node;

[Cmdlet("Set", "DscServerNodeScopeValue")]
public sealed class SetDscServerNodeScopeValueCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid NodeId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public Guid ScopeTypeId { get; set; }

    [Parameter(Mandatory = true, Position = 2)]
    [ValidateNotNullOrEmpty]
    public string ScopeValue { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<NodeHttpService>();
        var request = new SetNodeScopeValueRequest
        {
            ScopeTypeId = ScopeTypeId,
            ScopeValue = ScopeValue,
        };

        service.SetNodeScopeValueAsync(NodeId, request, CancellationToken.None).GetAwaiter().GetResult();
    }
}
