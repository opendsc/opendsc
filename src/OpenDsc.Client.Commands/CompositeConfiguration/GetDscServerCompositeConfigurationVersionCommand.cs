// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.CompositeConfigurations;

namespace OpenDsc.Client.Commands.CompositeConfiguration;

[Cmdlet(VerbsCommon.Get, "DscServerCompositeConfigurationVersion")]
[OutputType(typeof(CompositeConfigurationVersionDetails))]
public sealed class GetDscServerCompositeConfigurationVersionCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Version { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<CompositeConfigurationHttpService>();
        var result = service.GetVersionAsync(Name, Version, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
