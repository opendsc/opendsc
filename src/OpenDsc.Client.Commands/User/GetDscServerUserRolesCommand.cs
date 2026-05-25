// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.User;

[Cmdlet("Get", "DscServerUserRoles")]
[OutputType(typeof(RoleSummary))]
public sealed class GetDscServerUserRolesCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid UserId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<UserHttpService>();
        var result = service.GetUserRolesAsync(UserId, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
