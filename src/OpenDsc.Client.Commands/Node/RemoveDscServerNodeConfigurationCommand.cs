// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Node;

[Cmdlet(VerbsCommon.Remove, "DscServerNodeConfiguration", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public sealed class RemoveDscServerNodeConfigurationCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid NodeId { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(NodeId.ToString())) return;
        var service = GetRequiredService<NodeHttpService>();
        service.RemoveConfigurationAsync(NodeId, PipelineStopToken).GetAwaiter().GetResult();
    }
}
