// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Role;

[Cmdlet(VerbsCommon.Get, "DscServerRoleGroupCounts")]
[OutputType(typeof(Dictionary<Guid, int>))]
public sealed class GetDscServerRoleGroupCountsCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<RoleHttpService>();
        var result = service.GetRoleGroupCountsAsync(PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result);
    }
}
