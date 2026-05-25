// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.User;

[Cmdlet("Get", "DscServerUserGroupCounts")]
[OutputType(typeof(IReadOnlyDictionary<Guid, int>))]
public sealed class GetDscServerUserGroupCountsCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<UserHttpService>();
        var result = service.GetUserGroupCountsAsync(CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
