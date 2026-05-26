// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.Group;

[Cmdlet(VerbsCommon.New, "DscServerGroup", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(GroupSummary))]
public sealed class NewDscServerGroupCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = false, Position = 1)]
    public string? Description { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(Name)) return;
        var service = GetRequiredService<GroupHttpService>();
        var request = new CreateGroupRequest
        {
            Name = Name,
            Description = Description,
        };

        var result = service.CreateGroupAsync(request, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
