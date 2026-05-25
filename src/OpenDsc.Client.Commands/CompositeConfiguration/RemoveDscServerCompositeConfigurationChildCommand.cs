// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.CompositeConfiguration;

[Cmdlet("Remove", "DscServerCompositeConfigurationChild")]
public sealed class RemoveDscServerCompositeConfigurationChildCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ItemId { get; set; } = default;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<CompositeConfigurationHttpService>();
        service.RemoveChildAsync(ItemId, CancellationToken.None).GetAwaiter().GetResult();
    }
}
