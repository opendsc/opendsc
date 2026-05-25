// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Configurations;

namespace OpenDsc.Client.Commands.Configuration;

[Cmdlet("New", "DscServerConfigurationVersion")]
[OutputType(typeof(ConfigurationVersionDetails))]
public sealed class NewDscServerConfigurationVersionCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Version { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 2)]
    [ValidateNotNullOrEmpty]
    public FileUpload[] Files { get; set; } = [];

    [Parameter(Mandatory = false, Position = 3)]
    public string? EntryPoint { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ConfigurationHttpService>();
        var request = new CreateConfigurationVersionRequest
        {
            Version = Version,
            Files = Files,
            EntryPoint = EntryPoint,
        };
        var result = service.CreateVersionAsync(Name, request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
