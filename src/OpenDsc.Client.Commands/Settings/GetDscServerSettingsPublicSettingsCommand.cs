// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Lcm;

namespace OpenDsc.Client.Commands.Settings;

[Cmdlet(VerbsCommon.Get, "DscServerSettingsPublicSettings")]
[OutputType(typeof(PublicSettingsResponse))]
public sealed class GetDscServerSettingsPublicSettingsCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<SettingsHttpService>();
        var result = service.GetPublicSettingsAsync(PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
