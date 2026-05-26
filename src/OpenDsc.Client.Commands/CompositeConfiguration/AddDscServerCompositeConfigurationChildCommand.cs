// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Contracts.CompositeConfigurations;
using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.CompositeConfiguration;

[Cmdlet(VerbsCommon.Add, "DscServerCompositeConfigurationChild", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low)]
[OutputType(typeof(CompositeConfigurationItemDetails))]
public sealed class AddDscServerCompositeConfigurationChildCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = true)]
    [ValidateNotNullOrEmpty]
    public string Version { get; set; } = string.Empty;

    [Parameter(Mandatory = true)]
    [ValidateNotNullOrEmpty]
    public string ChildConfigurationName { get; set; } = string.Empty;

    [Parameter(Mandatory = true)]
    public int MajorVersion { get; set; }

    [Parameter(Mandatory = true)]
    public int Order { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(Name)) return;
        var service = GetRequiredService<CompositeConfigurationHttpService>();
        var request = new AddChildConfigurationRequest
        {
            ChildConfigurationName = ChildConfigurationName,
            MajorVersion = MajorVersion,
            Order = Order,
        };

        var result = service.AddChildAsync(Name, Version, request, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
