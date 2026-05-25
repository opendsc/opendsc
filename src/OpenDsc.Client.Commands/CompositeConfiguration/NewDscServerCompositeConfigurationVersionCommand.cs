// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.CompositeConfigurations;

namespace OpenDsc.Client.Commands.CompositeConfiguration;

[Cmdlet("New", "DscServerCompositeConfigurationVersion")]
[OutputType(typeof(CompositeConfigurationVersionDetails))]
public sealed class NewDscServerCompositeConfigurationVersionCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Version { get; set; } = string.Empty;

    [Parameter(Mandatory = false, Position = 2)]
    public string? PrereleaseChannel { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<CompositeConfigurationHttpService>();
        var request = new CreateCompositeConfigurationVersionRequest
        {
            Version = Version,
            PrereleaseChannel = PrereleaseChannel,
        };

        var result = service.CreateVersionAsync(Name, request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
