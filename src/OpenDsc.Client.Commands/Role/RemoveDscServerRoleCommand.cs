// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Role;

[Cmdlet("Remove", "DscServerRole")]
public sealed class RemoveDscServerRoleCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid RoleId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<RoleHttpService>();
        service.DeleteRoleAsync(RoleId, CancellationToken.None).GetAwaiter().GetResult();
    }
}
