// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.Scope;

[Cmdlet(VerbsLifecycle.Disable, "DscServerScopeType", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(ScopeTypeDetails))]
public sealed class DisableDscServerScopeTypeCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid Id { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(Id.ToString())) return;
        var service = GetRequiredService<ScopeHttpService>();
        var result = service.DisableScopeTypeAsync(Id, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
