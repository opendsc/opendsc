// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.Role;

[Cmdlet("New", "DscServerRole")]
[OutputType(typeof(RoleSummary))]
public sealed class NewDscServerRoleCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = false, Position = 1)]
    public string? Description { get; set; }

    [Parameter(Mandatory = true, Position = 2)]
    [ValidateNotNullOrEmpty]
    public string[] Permissions { get; set; } = [];

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<RoleHttpService>();
        var request = new CreateRoleRequest
        {
            Name = Name,
            Description = Description,
            Permissions = Permissions,
        };

        var result = service.CreateRoleAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
