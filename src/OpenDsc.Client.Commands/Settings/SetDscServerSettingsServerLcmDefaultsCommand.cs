// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Lcm;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.Settings;

[Cmdlet("Set", "DscServerSettingsServerLcmDefaults")]
[OutputType(typeof(ServerLcmDefaultsSummary))]
public sealed class SetDscServerSettingsServerLcmDefaultsCommand : DscServerCommandBase
{
    [Parameter(Mandatory = false, Position = 0)]
    public ConfigurationMode? DefaultConfigurationMode { get; set; }

    [Parameter(Mandatory = false, Position = 1)]
    public TimeSpan? DefaultConfigurationModeInterval { get; set; }

    [Parameter(Mandatory = false, Position = 2)]
    public bool? DefaultReportCompliance { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<SettingsHttpService>();
        var request = new UpdateServerLcmDefaultsRequest
        {
            DefaultConfigurationMode = DefaultConfigurationMode,
            DefaultConfigurationModeInterval = DefaultConfigurationModeInterval,
            DefaultReportCompliance = DefaultReportCompliance,
        };

        var result = service.UpdateServerLcmDefaultsAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
