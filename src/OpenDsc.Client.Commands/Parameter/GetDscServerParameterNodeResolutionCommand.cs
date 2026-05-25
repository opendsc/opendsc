// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Parameters;

namespace OpenDsc.Client.Commands.Parameter;

[Cmdlet("Get", "DscServerParameterNodeResolution")]
[OutputType(typeof(ParameterResolutionDetails))]
public sealed class GetDscServerParameterNodeResolutionCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid NodeId { get; set; }

    [Parameter(Mandatory = false, Position = 1)]
    public Guid? ConfigurationId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ParameterHttpService>();
        var result = service.GetNodeResolutionAsync(NodeId, ConfigurationId, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
