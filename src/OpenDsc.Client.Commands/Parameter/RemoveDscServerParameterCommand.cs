// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Parameter;

[Cmdlet(VerbsCommon.Remove, "DscServerParameter", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public sealed class RemoveDscServerParameterCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ScopeTypeId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public Guid ConfigurationId { get; set; }

    [Parameter(Mandatory = true, Position = 2)]
    [ValidateNotNullOrEmpty]
    public string ScopeValue { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 3)]
    [ValidateNotNullOrEmpty]
    public string Version { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(ScopeTypeId.ToString())) return;
        var service = GetRequiredService<ParameterHttpService>();
        service.DeleteAsync(ScopeTypeId, ConfigurationId, ScopeValue, Version, PipelineStopToken).GetAwaiter().GetResult();
    }
}
