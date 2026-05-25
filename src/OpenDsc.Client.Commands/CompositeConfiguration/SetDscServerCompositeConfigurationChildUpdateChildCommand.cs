// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.CompositeConfigurations;

namespace OpenDsc.Client.Commands.CompositeConfiguration;

[Cmdlet("Set", "DscServerCompositeConfigurationChildUpdateChild")]
[OutputType(typeof(CompositeConfigurationItemDetails))]
public sealed class SetDscServerCompositeConfigurationChildUpdateChildCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ItemId { get; set; } = default;

    [Parameter(Mandatory = false, Position = 1)]
    public string? ActiveVersion { get; set; }

    [Parameter(Mandatory = true, Position = 2)]
    public int Order { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<CompositeConfigurationHttpService>();
        var request = new UpdateChildConfigurationRequest
        {
            ActiveVersion = ActiveVersion,
            Order = Order,
        };

        var result = service.UpdateChildAsync(ItemId, request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
