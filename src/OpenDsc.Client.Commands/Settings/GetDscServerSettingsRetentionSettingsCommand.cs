// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.Settings;

[Cmdlet("Get", "DscServerSettingsRetentionSettings")]
[OutputType(typeof(RetentionSettingsSummary))]
public sealed class GetDscServerSettingsRetentionSettingsCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<SettingsHttpService>();
        var result = service.GetRetentionSettingsAsync(CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
