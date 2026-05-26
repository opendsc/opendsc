// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Group;

[Cmdlet(VerbsCommon.Get, "DscServerGroupMemberCounts")]
[OutputType(typeof(Dictionary<Guid, int>))]
public sealed class GetDscServerGroupMemberCountsCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<GroupHttpService>();
        var result = service.GetGroupMemberCountsAsync(PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result);
    }
}
