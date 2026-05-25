// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Nodes;

namespace OpenDsc.Client.Commands.Node;

[Cmdlet("Get", "DscServerNodeConfigurationManifest")]
[OutputType(typeof(NodeConfigurationManifest))]
public sealed class GetDscServerNodeConfigurationManifestCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid NodeId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<NodeHttpService>();
        var result = service.GetNodeConfigurationManifestAsync(NodeId, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
