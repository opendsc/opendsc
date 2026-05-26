// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.User;

[Cmdlet(VerbsCommon.Get, "DscServerUserGroupCounts")]
[OutputType(typeof(Dictionary<Guid, int>))]
public sealed class GetDscServerUserGroupCountsCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<UserHttpService>();
        var result = service.GetUserGroupCountsAsync(PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result);
    }
}
