// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.Group;

[Cmdlet("Set", "DscServerGroup")]
[OutputType(typeof(GroupSummary))]
public sealed class SetDscServerGroupCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid GroupId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = false, Position = 2)]
    public string? Description { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<GroupHttpService>();
        var request = new UpdateGroupRequest
        {
            Name = Name,
            Description = Description,
        };

        var result = service.UpdateGroupAsync(GroupId, request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
