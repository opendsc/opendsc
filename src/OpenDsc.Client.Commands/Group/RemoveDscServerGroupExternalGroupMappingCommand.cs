// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Group;

[Cmdlet("Remove", "DscServerGroupExternalGroupMapping")]
public sealed class RemoveDscServerGroupExternalGroupMappingCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid MappingId { get; set; } = default;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<GroupHttpService>();
        service.DeleteExternalGroupMappingAsync(MappingId, CancellationToken.None).GetAwaiter().GetResult();
    }
}
