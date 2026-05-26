// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.CompositeConfigurations;

namespace OpenDsc.Client.Commands.CompositeConfiguration;

[Cmdlet(VerbsCommon.Get, "DscServerCompositeConfigurationAvailableChildConfigurations")]
[OutputType(typeof(ChildConfigurationOption))]
public sealed class GetDscServerCompositeConfigurationAvailableChildConfigurationsCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public IEnumerable<Guid> ExcludeIds { get; set; } = null!;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<CompositeConfigurationHttpService>();
        var result = service.GetAvailableChildConfigurationsAsync(ExcludeIds, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
