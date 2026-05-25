// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Configuration;

[Cmdlet("Test", "DscServerConfigurationAssigned")]
[OutputType(typeof(bool))]
public sealed class TestDscServerConfigurationAssignedCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ConfigurationHttpService>();
        var result = service.IsConfigurationAssignedAsync(Name, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
