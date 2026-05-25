// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Configurations;

namespace OpenDsc.Client.Commands.Configuration;

[Cmdlet("Add", "DscServerConfigurationFiles")]
public sealed class AddDscServerConfigurationFilesCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Version { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 2)]
    public FileUpload[] Files { get; set; } = [];

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ConfigurationHttpService>();
        service.AddFilesAsync(Name, Version, Files, CancellationToken.None).GetAwaiter().GetResult();
    }
}
