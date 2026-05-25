// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Parameters;

namespace OpenDsc.Client.Commands.Parameter;

[Cmdlet("Get", "DscServerParameterMajorVersionSummaries")]
[OutputType(typeof(MajorVersionSummary))]
public sealed class GetDscServerParameterMajorVersionSummariesCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ScopeTypeId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public Guid ConfigurationId { get; set; }

    [Parameter(Mandatory = false, Position = 2)]
    public string ScopeValue { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ParameterHttpService>();
        var result = service.GetMajorVersionSummariesAsync(ScopeTypeId, ConfigurationId, ScopeValue, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
