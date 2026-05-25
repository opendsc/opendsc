// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.User;

[Cmdlet("Set", "DscServerUserRole")]
public sealed class SetDscServerUserRoleCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid UserId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public Guid RoleId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<UserHttpService>();
        var request = new AssignRoleRequest
        {
            RoleId = RoleId,
        };

        service.AssignRoleAsync(UserId, request, CancellationToken.None).GetAwaiter().GetResult();
    }
}
