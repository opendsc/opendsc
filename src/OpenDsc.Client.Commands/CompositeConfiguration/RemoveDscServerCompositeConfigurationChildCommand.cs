// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.CompositeConfiguration;

[Cmdlet(VerbsCommon.Remove, "DscServerCompositeConfigurationChild", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public sealed class RemoveDscServerCompositeConfigurationChildCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ItemId { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(ItemId.ToString())) return;
        var service = GetRequiredService<CompositeConfigurationHttpService>();
        service.RemoveChildAsync(ItemId, PipelineStopToken).GetAwaiter().GetResult();
    }
}
