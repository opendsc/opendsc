// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.CompositeConfigurations;

namespace OpenDsc.Client.Commands.CompositeConfiguration;

[Cmdlet("New", "DscServerCompositeConfiguration")]
[OutputType(typeof(CompositeConfigurationDetails))]
public sealed class NewDscServerCompositeConfigurationCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = false, Position = 1)]
    public string? Description { get; set; }

    [Parameter(Mandatory = true, Position = 2)]
    [ValidateNotNullOrEmpty]
    public string EntryPoint { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<CompositeConfigurationHttpService>();
        var request = new CreateCompositeConfigurationRequest
        {
            Name = Name,
            Description = Description,
            EntryPoint = EntryPoint,
        };

        var result = service.CreateAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
