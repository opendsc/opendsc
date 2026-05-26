// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Group;

[Cmdlet(VerbsCommon.Remove, "DscServerGroup", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public sealed class RemoveDscServerGroupCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid GroupId { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(GroupId.ToString())) return;
        var service = GetRequiredService<GroupHttpService>();
        service.DeleteGroupAsync(GroupId, PipelineStopToken).GetAwaiter().GetResult();
    }
}
