// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.Group;

[Cmdlet("Set", "DscServerGroupMembers")]
public sealed class SetDscServerGroupMembersCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid GroupId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public Guid[] UserIds { get; set; } = [];

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<GroupHttpService>();
        var request = new SetGroupMembersRequest
        {
            UserIds = UserIds,
        };

        service.SetMembersAsync(GroupId, request, CancellationToken.None).GetAwaiter().GetResult();
    }
}
