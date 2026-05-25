// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Retention;

namespace OpenDsc.Client.Commands.Settings;

[Cmdlet("Get", "DscServerSettingsRetentionHistory")]
[OutputType(typeof(RetentionRunSummary))]
public sealed class GetDscServerSettingsRetentionHistoryCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<SettingsHttpService>();
        var result = service.GetRetentionHistoryAsync(CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
