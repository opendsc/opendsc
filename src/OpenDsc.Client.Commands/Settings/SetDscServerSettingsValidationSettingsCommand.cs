// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.Settings;

[Cmdlet(VerbsCommon.Set, "DscServerSettingsValidationSettings", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(ValidationSettingsSummary))]
public sealed class SetDscServerSettingsValidationSettingsCommand : DscServerCommandBase
{
    [Parameter(Mandatory = false, Position = 0)]
    public bool? RequireSemVer { get; set; }

    [Parameter(Mandatory = false, Position = 1)]
    public ParameterValidationMode? DefaultParameterValidationMode { get; set; }

    [Parameter(Mandatory = false, Position = 2)]
    public bool? AllowConfigurationOverride { get; set; }

    [Parameter(Mandatory = false, Position = 3)]
    public bool? AllowParameterValidationOverride { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(string.Empty)) return;
        var service = GetRequiredService<SettingsHttpService>();
        var request = new UpdateValidationSettingsRequest
        {
            RequireSemVer = RequireSemVer,
            DefaultParameterValidationMode = DefaultParameterValidationMode,
            AllowConfigurationOverride = AllowConfigurationOverride,
            AllowParameterValidationOverride = AllowParameterValidationOverride,
        };

        var result = service.UpdateValidationSettingsAsync(request, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
