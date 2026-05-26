// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.Role;

[Cmdlet(VerbsCommon.Set, "DscServerRoleGroupsForRole", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
public sealed class SetDscServerRoleGroupsForRoleCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid RoleId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public Guid[] GroupIds { get; set; } = [];

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(RoleId.ToString())) return;
        var service = GetRequiredService<RoleHttpService>();
        var request = new SetRoleGroupsRequest
        {
            GroupIds = GroupIds,
        };

        service.SetGroupsForRoleAsync(RoleId, request, PipelineStopToken).GetAwaiter().GetResult();
    }
}
