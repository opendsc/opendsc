// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Client.Commands.Parameter;

[Cmdlet(VerbsSecurity.Grant, "DscServerParameterPermission", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public sealed class GrantDscServerParameterPermissionCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ConfigurationId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public PrincipalType PrincipalType { get; set; }

    [Parameter(Mandatory = true, Position = 2)]
    public Guid PrincipalId { get; set; }

    [Parameter(Mandatory = true, Position = 3)]
    public ResourcePermission Level { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(ConfigurationId.ToString())) return;
        var service = GetRequiredService<ParameterHttpService>();
        var request = new GrantPermissionRequest
        {
            PrincipalType = PrincipalType,
            PrincipalId = PrincipalId,
            Level = Level,
        };

        service.GrantPermissionAsync(ConfigurationId, request, PipelineStopToken).GetAwaiter().GetResult();
    }
}
