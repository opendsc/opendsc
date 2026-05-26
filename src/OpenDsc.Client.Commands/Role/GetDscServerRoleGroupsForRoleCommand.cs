// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.Role;

[Cmdlet(VerbsCommon.Get, "DscServerRoleGroupsForRole")]
[OutputType(typeof(GroupSummary))]
public sealed class GetDscServerRoleGroupsForRoleCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid RoleId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<RoleHttpService>();
        var result = service.GetGroupsForRoleAsync(RoleId, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
