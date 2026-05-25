// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Reports;
using OpenDsc.Schema;

namespace OpenDsc.Client.Commands.Report;

[Cmdlet("Submit", "DscServerReport")]
[OutputType(typeof(ReportSummary))]
public sealed class SubmitDscServerReportCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid NodeId { get; set; } = default; [Parameter(Mandatory = true, Position = 1)]
    public DscOperation Operation { get; set; }

    [Parameter(Mandatory = true, Position = 2)]
    public DscResult Result { get; set; } = null!;
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ReportHttpService>();
        var request = new SubmitReportRequest
        {
            Operation = Operation,
            Result = Result,
        };
        var result = service.SubmitReportAsync(NodeId, request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
