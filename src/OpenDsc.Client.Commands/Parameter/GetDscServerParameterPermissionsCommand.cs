// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Client.Commands.Parameter;

[Cmdlet(VerbsCommon.Get, "DscServerParameterPermissions")]
[OutputType(typeof(PermissionEntry))]
public sealed class GetDscServerParameterPermissionsCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ConfigurationId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ParameterHttpService>();
        var result = service.GetPermissionsAsync(ConfigurationId, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
