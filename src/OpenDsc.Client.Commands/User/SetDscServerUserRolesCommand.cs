// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.User;

[Cmdlet("Set", "DscServerUserRoles")]
public sealed class SetDscServerUserRolesCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid UserId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public Guid[] RoleIds { get; set; } = [];

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<UserHttpService>();
        var request = new SetUserRolesRequest
        {
            RoleIds = RoleIds,
        };

        service.SetUserRolesAsync(UserId, request, CancellationToken.None).GetAwaiter().GetResult();
    }
}
