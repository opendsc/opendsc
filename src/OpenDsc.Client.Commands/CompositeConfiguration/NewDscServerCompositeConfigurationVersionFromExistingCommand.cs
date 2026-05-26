// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Configurations;

namespace OpenDsc.Client.Commands.CompositeConfiguration;

[Cmdlet(VerbsCommon.New, "DscServerCompositeConfigurationVersionFromExisting", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
public sealed class NewDscServerCompositeConfigurationVersionFromExistingCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string SourceVersion { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 2)]
    [ValidateNotNullOrEmpty]
    public string NewVersion { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(Name)) return;
        var service = GetRequiredService<CompositeConfigurationHttpService>();
        var request = new CreateCompositeVersionFromExistingRequest
        {
            SourceVersion = SourceVersion,
            NewVersion = NewVersion,
        };

        service.CreateVersionFromExistingAsync(Name, request, PipelineStopToken).GetAwaiter().GetResult();
    }
}
