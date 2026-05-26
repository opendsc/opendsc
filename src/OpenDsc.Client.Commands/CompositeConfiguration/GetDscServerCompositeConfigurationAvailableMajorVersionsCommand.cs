// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.CompositeConfiguration;

[Cmdlet(VerbsCommon.Get, "DscServerCompositeConfigurationAvailableMajorVersions")]
[OutputType(typeof(int))]
public sealed class GetDscServerCompositeConfigurationAvailableMajorVersionsCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ConfigurationId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<CompositeConfigurationHttpService>();
        var result = service.GetAvailableMajorVersionsAsync(ConfigurationId, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
