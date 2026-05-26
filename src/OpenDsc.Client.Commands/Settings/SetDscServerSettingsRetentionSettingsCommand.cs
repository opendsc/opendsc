// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.Settings;

[Cmdlet(VerbsCommon.Set, "DscServerSettingsRetentionSettings", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(RetentionSettingsSummary))]
public sealed class SetDscServerSettingsRetentionSettingsCommand : DscServerCommandBase
{
    [Parameter(Mandatory = false, Position = 0)]
    public bool? Enabled { get; set; }

    [Parameter(Mandatory = false, Position = 1)]
    public int? KeepVersions { get; set; }

    [Parameter(Mandatory = false, Position = 2)]
    public int? KeepDays { get; set; }

    [Parameter(Mandatory = false, Position = 3)]
    public bool? KeepReleaseVersions { get; set; }

    [Parameter(Mandatory = false, Position = 4)]
    public int? ScheduleIntervalHours { get; set; }

    [Parameter(Mandatory = false, Position = 5)]
    public int? ReportKeepCount { get; set; }

    [Parameter(Mandatory = false, Position = 6)]
    public int? ReportKeepDays { get; set; }

    [Parameter(Mandatory = false, Position = 7)]
    public int? StatusEventKeepCount { get; set; }

    [Parameter(Mandatory = false, Position = 8)]
    public int? StatusEventKeepDays { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(string.Empty)) return;
        var service = GetRequiredService<SettingsHttpService>();
        var request = new UpdateRetentionSettingsRequest
        {
            Enabled = Enabled,
            KeepVersions = KeepVersions,
            KeepDays = KeepDays,
            KeepReleaseVersions = KeepReleaseVersions,
            ScheduleIntervalHours = ScheduleIntervalHours,
            ReportKeepCount = ReportKeepCount,
            ReportKeepDays = ReportKeepDays,
            StatusEventKeepCount = StatusEventKeepCount,
            StatusEventKeepDays = StatusEventKeepDays,
        };

        var result = service.UpdateRetentionSettingsAsync(request, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
