// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Client.Commands.Configuration;

[Cmdlet(VerbsCommon.Get, "DscServerConfigurationPermissions")]
[OutputType(typeof(PermissionEntry))]
public sealed class GetDscServerConfigurationPermissionsCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ConfigurationHttpService>();
        var result = service.GetPermissionsAsync(Name, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
