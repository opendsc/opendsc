// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Reports;

namespace OpenDsc.Client.Commands.Report;

[Cmdlet("Get", "DscServerReport")]
[OutputType(typeof(ReportDetails))]
public sealed class GetDscServerReportCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ReportId { get; set; } = default;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ReportHttpService>();
        var result = service.GetReportAsync(ReportId, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
