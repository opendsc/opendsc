// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.Role;

[Cmdlet("Get", "DscServerRoleGetRoles")]
[OutputType(typeof(RoleSummary))]
public sealed class GetDscServerRoleGetRolesCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<RoleHttpService>();
        var result = service.GetRolesAsync(CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
