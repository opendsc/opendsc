// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Configuration;

[Cmdlet("Reset", "DscServerConfigurationRetentionSettings")]
public sealed class ResetDscServerConfigurationRetentionSettingsCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ConfigurationHttpService>();
        service.ResetRetentionSettingsAsync(Name, CancellationToken.None).GetAwaiter().GetResult();
    }
}
