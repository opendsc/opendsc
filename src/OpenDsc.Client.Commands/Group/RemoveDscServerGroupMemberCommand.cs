// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.Group;

[Cmdlet("Remove", "DscServerGroupMember")]
public sealed class RemoveDscServerGroupMemberCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid GroupId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public Guid UserId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<GroupHttpService>();
        var request = new RemoveGroupMemberRequest
        {
            UserId = UserId,
        };

        service.RemoveMemberAsync(GroupId, request, CancellationToken.None).GetAwaiter().GetResult();
    }
}
