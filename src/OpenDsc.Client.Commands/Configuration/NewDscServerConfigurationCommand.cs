// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Configurations;

namespace OpenDsc.Client.Commands.Configuration;

[Cmdlet(VerbsCommon.New, "DscServerConfiguration", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(ConfigurationDetails))]
public sealed class NewDscServerConfigurationCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = false, Position = 1)]
    public string? Description { get; set; }

    [Parameter(Mandatory = true, Position = 2)]
    [ValidateNotNullOrEmpty]
    public string EntryPoint { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 3)]
    [ValidateNotNullOrEmpty]
    public string Version { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 4)]
    public bool UseServerManagedParameters { get; set; }

    [Parameter(Mandatory = true, Position = 5)]
    [ValidateNotNullOrEmpty]
    public FileUpload[] Files { get; set; } = [];

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(Name)) return;
        var service = GetRequiredService<ConfigurationHttpService>();
        var request = new CreateConfigurationAdminRequest
        {
            Name = Name,
            Description = Description,
            EntryPoint = EntryPoint,
            Version = Version,
            UseServerManagedParameters = UseServerManagedParameters,
            Files = Files,
        };

        var result = service.CreateAsync(request, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
