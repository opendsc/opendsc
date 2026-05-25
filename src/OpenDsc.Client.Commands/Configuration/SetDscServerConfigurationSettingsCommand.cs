// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Configurations;

namespace OpenDsc.Client.Commands.Configuration;

[Cmdlet("Set", "DscServerConfigurationSettings")]
[OutputType(typeof(ConfigurationSettingsSummary))]
public sealed class SetDscServerConfigurationSettingsCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = false, Position = 1)]
    public bool? RequireSemVer { get; set; }

    [Parameter(Mandatory = false, Position = 2)]
    public ParameterValidationMode? ParameterValidationMode { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ConfigurationHttpService>();
        var request = new UpdateConfigurationSettingsRequest
        {
            RequireSemVer = RequireSemVer,
            ParameterValidationMode = ParameterValidationMode,
        };

        var result = service.UpdateSettingsAsync(Name, request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
