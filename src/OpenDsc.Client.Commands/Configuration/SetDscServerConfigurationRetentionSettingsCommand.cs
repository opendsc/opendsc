// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Configurations;

namespace OpenDsc.Client.Commands.Configuration;

[Cmdlet(VerbsCommon.Set, "DscServerConfigurationRetentionSettings", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
public sealed class SetDscServerConfigurationRetentionSettingsCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = false, Position = 1)]
    public bool? Enabled { get; set; }

    [Parameter(Mandatory = false, Position = 2)]
    public int? KeepVersions { get; set; }

    [Parameter(Mandatory = false, Position = 3)]
    public int? KeepDays { get; set; }

    [Parameter(Mandatory = false, Position = 4)]
    public bool? KeepReleaseVersions { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(Name)) return;
        var service = GetRequiredService<ConfigurationHttpService>();
        var request = new SaveRetentionSettingsRequest
        {
            Enabled = Enabled,
            KeepVersions = KeepVersions,
            KeepDays = KeepDays,
            KeepReleaseVersions = KeepReleaseVersions,
        };

        service.SaveRetentionSettingsAsync(Name, request, PipelineStopToken).GetAwaiter().GetResult();
    }
}
