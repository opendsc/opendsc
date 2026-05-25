// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Parameter;

[Cmdlet("Publish", "DscServerParameter")]
public sealed class PublishDscServerParameterCommand : DscServerCommandBase
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
        var service = GetRequiredService<ParameterHttpService>();
        service.PublishAsync(ScopeTypeId, ConfigurationId, ScopeValue, Version, CancellationToken.None).GetAwaiter().GetResult();
    }
}
