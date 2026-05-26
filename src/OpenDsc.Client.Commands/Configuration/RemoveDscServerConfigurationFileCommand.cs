// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Configuration;

[Cmdlet(VerbsCommon.Remove, "DscServerConfigurationFile", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public sealed class RemoveDscServerConfigurationFileCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Version { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 2)]
    [ValidateNotNullOrEmpty]
    public string FilePath { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(Name)) return;
        var service = GetRequiredService<ConfigurationHttpService>();
        service.DeleteFileAsync(Name, Version, FilePath, PipelineStopToken).GetAwaiter().GetResult();
    }
}
