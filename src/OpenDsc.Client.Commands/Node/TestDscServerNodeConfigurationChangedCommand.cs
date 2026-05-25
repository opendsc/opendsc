// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Node;

[Cmdlet("Test", "DscServerNodeConfigurationChanged")]
[OutputType(typeof(bool))]
public sealed class TestDscServerNodeConfigurationChangedCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid NodeId { get; set; } = default;

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Etag { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<NodeHttpService>();
        var result = service.CheckConfigurationChangedAsync(NodeId, Etag, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
