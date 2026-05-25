// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Group;

[Cmdlet("Get", "DscServerGroupRoleCounts")]
[OutputType(typeof(IReadOnlyDictionary<Guid, int>))]
public sealed class GetDscServerGroupRoleCountsCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<GroupHttpService>();
        var result = service.GetGroupRoleCountsAsync(CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
