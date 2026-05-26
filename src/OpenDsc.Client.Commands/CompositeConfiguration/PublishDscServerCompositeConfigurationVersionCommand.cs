// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.CompositeConfiguration;

[Cmdlet(VerbsData.Publish, "DscServerCompositeConfigurationVersion", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
public sealed class PublishDscServerCompositeConfigurationVersionCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Version { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(Name)) return;
        var service = GetRequiredService<CompositeConfigurationHttpService>();
        service.PublishVersionAsync(Name, Version, PipelineStopToken).GetAwaiter().GetResult();
    }
}
