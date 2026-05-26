// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Reports;

namespace OpenDsc.Client.Commands.Report;

[Cmdlet(VerbsCommon.Get, "DscServerReportGetReports")]
[OutputType(typeof(ReportSummary))]
public sealed class GetDscServerReportGetReportsCommand : DscServerCommandBase
{
    [Parameter(Mandatory = false, Position = 0)]
    public Guid? NodeId { get; set; } = null!;

    [Parameter(Mandatory = false, Position = 1)]
    public int? Skip { get; set; } = null!;

    [Parameter(Mandatory = false, Position = 2)]
    public int? Take { get; set; } = null!;

    [Parameter(Mandatory = false, Position = 3)]
    public DateTimeOffset? From { get; set; } = null!;

    [Parameter(Mandatory = false, Position = 4)]
    public DateTimeOffset? To { get; set; } = null!;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ReportHttpService>();
        var result = service.GetReportsAsync(NodeId, Skip, Take, From, To, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
