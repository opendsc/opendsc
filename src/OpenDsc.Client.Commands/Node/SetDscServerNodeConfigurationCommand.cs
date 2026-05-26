// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Nodes;

namespace OpenDsc.Client.Commands.Node;

[Cmdlet(VerbsCommon.Set, "DscServerNodeConfiguration", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
public sealed class SetDscServerNodeConfigurationCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid NodeId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string ConfigurationName { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 2)]
    public bool IsComposite { get; set; }

    [Parameter(Mandatory = false, Position = 3)]
    public int? MajorVersion { get; set; }

    [Parameter(Mandatory = false, Position = 4)]
    public string? PrereleaseChannel { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(NodeId.ToString())) return;
        var service = GetRequiredService<NodeHttpService>();
        var request = new AssignConfigurationRequest
        {
            ConfigurationName = ConfigurationName,
            IsComposite = IsComposite,
            MajorVersion = MajorVersion,
            PrereleaseChannel = PrereleaseChannel,
        };

        service.AssignConfigurationAsync(NodeId, request, PipelineStopToken).GetAwaiter().GetResult();
    }
}
