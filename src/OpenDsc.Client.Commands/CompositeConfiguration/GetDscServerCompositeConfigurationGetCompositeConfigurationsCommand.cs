// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.CompositeConfigurations;

namespace OpenDsc.Client.Commands.CompositeConfiguration;

[Cmdlet("Get", "DscServerCompositeConfigurationGetCompositeConfigurations")]
[OutputType(typeof(CompositeConfigurationSummary))]
public sealed class GetDscServerCompositeConfigurationGetCompositeConfigurationsCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<CompositeConfigurationHttpService>();
        var result = service.GetCompositeConfigurationsAsync(CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
