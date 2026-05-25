// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.Role;

[Cmdlet("Set", "DscServerRole")]
[OutputType(typeof(RoleSummary))]
public sealed class SetDscServerRoleCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid RoleId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = false, Position = 2)]
    public string? Description { get; set; }

    [Parameter(Mandatory = true, Position = 3)]
    [ValidateNotNullOrEmpty]
    public string[] Permissions { get; set; } = [];

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<RoleHttpService>();
        var request = new UpdateRoleRequest
        {
            Name = Name,
            Description = Description,
            Permissions = Permissions,
        };

        var result = service.UpdateRoleAsync(RoleId, request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
