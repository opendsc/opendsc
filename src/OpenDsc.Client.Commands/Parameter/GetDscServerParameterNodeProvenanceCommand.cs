// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Parameters;

namespace OpenDsc.Client.Commands.Parameter;

[Cmdlet(VerbsCommon.Get, "DscServerParameterNodeProvenance")]
[OutputType(typeof(ParameterProvenanceDetails))]
public sealed class GetDscServerParameterNodeProvenanceCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid NodeId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public Guid ConfigurationId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ParameterHttpService>();
        var result = service.GetNodeProvenanceAsync(NodeId, ConfigurationId, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
