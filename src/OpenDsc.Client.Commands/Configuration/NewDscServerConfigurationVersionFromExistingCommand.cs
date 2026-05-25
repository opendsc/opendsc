// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Configurations;

namespace OpenDsc.Client.Commands.Configuration;

[Cmdlet("New", "DscServerConfigurationVersionFromExisting")]
[OutputType(typeof(ConfigurationVersionDetails))]
public sealed class NewDscServerConfigurationVersionFromExistingCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string SourceVersion { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 2)]
    [ValidateNotNullOrEmpty]
    public string NewVersion { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ConfigurationHttpService>();
        var request = new CreateVersionFromExistingRequest
        {
            SourceVersion = SourceVersion,
            NewVersion = NewVersion,
        };

        var result = service.CreateVersionFromExistingAsync(Name, request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
