// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Group;

[Cmdlet("Get", "DscServerGroupMembers")]
[OutputType(typeof(IReadOnlyList<Contracts.Users.UserSummary>))]
public sealed class GetDscServerGroupMembersCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid GroupId { get; set; } = default;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<GroupHttpService>();
        var result = service.GetGroupMembersAsync(GroupId, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
