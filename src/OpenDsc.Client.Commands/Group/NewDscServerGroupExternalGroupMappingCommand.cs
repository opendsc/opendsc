// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.Group;

[Cmdlet("New", "DscServerGroupExternalGroupMapping")]
[OutputType(typeof(ExternalGroupMappingInfo))]
public sealed class NewDscServerGroupExternalGroupMappingCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Provider { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string ExternalGroupId { get; set; } = string.Empty;

    [Parameter(Mandatory = false, Position = 2)]
    public string? ExternalGroupName { get; set; }

    [Parameter(Mandatory = true, Position = 3)]
    public Guid GroupId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<GroupHttpService>();
        var request = new CreateExternalGroupMappingRequest
        {
            Provider = Provider,
            ExternalGroupId = ExternalGroupId,
            ExternalGroupName = ExternalGroupName,
            GroupId = GroupId,
        };

        var result = service.CreateExternalGroupMappingAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
