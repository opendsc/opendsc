// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Group;

[Cmdlet("Remove", "DscServerGroup")]
public sealed class RemoveDscServerGroupCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid GroupId { get; set; } = default;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<GroupHttpService>();
        service.DeleteGroupAsync(GroupId, CancellationToken.None).GetAwaiter().GetResult();
    }
}
