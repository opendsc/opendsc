// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Client.Commands.Configuration;

[Cmdlet("Grant", "DscServerConfigurationPermission")]
public sealed class GrantDscServerConfigurationPermissionCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    public PrincipalType PrincipalType { get; set; }

    [Parameter(Mandatory = true, Position = 2)]
    public Guid PrincipalId { get; set; }

    [Parameter(Mandatory = true, Position = 3)]
    public ResourcePermission Level { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ConfigurationHttpService>();
        var request = new GrantPermissionRequest
        {
            PrincipalType = PrincipalType,
            PrincipalId = PrincipalId,
            Level = Level,
        };

        service.GrantPermissionAsync(Name, request, CancellationToken.None).GetAwaiter().GetResult();
    }
}
