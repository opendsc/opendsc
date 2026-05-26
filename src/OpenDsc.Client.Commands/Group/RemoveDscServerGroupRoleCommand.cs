// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.Group;

[Cmdlet(VerbsCommon.Remove, "DscServerGroupRole", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public sealed class RemoveDscServerGroupRoleCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid GroupId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public Guid RoleId { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(GroupId.ToString())) return;
        var service = GetRequiredService<GroupHttpService>();
        var request = new RemoveGroupRoleRequest
        {
            RoleId = RoleId,
        };

        service.RemoveRoleAsync(GroupId, request, PipelineStopToken).GetAwaiter().GetResult();
    }
}
