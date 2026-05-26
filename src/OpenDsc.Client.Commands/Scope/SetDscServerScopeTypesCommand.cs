// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.Scope;

[Cmdlet(VerbsCommon.Set, "DscServerScopeTypes", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(ScopeTypeDetails))]
public sealed class SetDscServerScopeTypesCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public Guid[] ScopeTypeIds { get; set; } = [];

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(ScopeTypeIds.ToString())) return;
        var service = GetRequiredService<ScopeHttpService>();
        var request = new ReorderScopeTypesRequest
        {
            ScopeTypeIds = ScopeTypeIds,
        };

        var result = service.ReorderScopeTypesAsync(request, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
