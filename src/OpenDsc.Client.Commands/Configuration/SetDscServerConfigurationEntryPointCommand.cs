// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Configuration;

[Cmdlet("Set", "DscServerConfigurationEntryPoint")]
public sealed class SetDscServerConfigurationEntryPointCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Version { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 2)]
    [ValidateNotNullOrEmpty]
    public string EntryPoint { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ConfigurationHttpService>();
        service.ChangeEntryPointAsync(Name, Version, EntryPoint, CancellationToken.None).GetAwaiter().GetResult();
    }
}
