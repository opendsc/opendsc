// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Client.Commands.Configuration;

[Cmdlet(VerbsSecurity.Revoke, "DscServerConfigurationPermission", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public sealed class RevokeDscServerConfigurationPermissionCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    public Guid PrincipalId { get; set; }

    [Parameter(Mandatory = true, Position = 2)]
    public PrincipalType PrincipalType { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(Name)) return;
        var service = GetRequiredService<ConfigurationHttpService>();
        var request = new RevokePermissionRequest
        {
            PrincipalId = PrincipalId,
            PrincipalType = PrincipalType,
        };

        service.RevokePermissionAsync(Name, request, PipelineStopToken).GetAwaiter().GetResult();
    }
}
