// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.User;

[Cmdlet("Get", "DscServerUserGetUsers")]
[OutputType(typeof(UserSummary))]
public sealed class GetDscServerUserGetUsersCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<UserHttpService>();
        var result = service.GetUsersAsync(CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
