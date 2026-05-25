// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Client.Commands.Parameter;

[Cmdlet("Revoke", "DscServerParameterPermission")]
public sealed class RevokeDscServerParameterPermissionCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ConfigurationId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public Guid PrincipalId { get; set; }

    [Parameter(Mandatory = true, Position = 2)]
    public PrincipalType PrincipalType { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ParameterHttpService>();
        var request = new RevokePermissionRequest
        {
            PrincipalId = PrincipalId,
            PrincipalType = PrincipalType,
        };

        service.RevokePermissionAsync(ConfigurationId, request, CancellationToken.None).GetAwaiter().GetResult();
    }
}
