// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Nodes;

namespace OpenDsc.Client.Commands.Node;

[Cmdlet("Get", "DscServerNodeAvailableConfigurations")]
[OutputType(typeof(ConfigurationOption))]
public sealed class GetDscServerNodeAvailableConfigurationsCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<NodeHttpService>();
        var result = service.GetAvailableConfigurationsAsync(CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
