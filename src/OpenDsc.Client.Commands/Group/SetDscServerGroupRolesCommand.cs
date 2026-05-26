// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.Group;

[Cmdlet(VerbsCommon.Set, "DscServerGroupRoles", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
public sealed class SetDscServerGroupRolesCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid GroupId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public Guid[] RoleIds { get; set; } = [];

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(GroupId.ToString())) return;
        var service = GetRequiredService<GroupHttpService>();
        var request = new SetGroupRolesRequest
        {
            RoleIds = RoleIds,
        };

        service.SetRolesAsync(GroupId, request, PipelineStopToken).GetAwaiter().GetResult();
    }
}
