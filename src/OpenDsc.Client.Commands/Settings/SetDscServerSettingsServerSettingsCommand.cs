// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.Settings;

[Cmdlet("Set", "DscServerSettingsServerSettings")]
[OutputType(typeof(ServerSettingsSummary))]
public sealed class SetDscServerSettingsServerSettingsCommand : DscServerCommandBase
{
    [Parameter(Mandatory = false, Position = 0)]
    public TimeSpan? CertificateRotationInterval { get; set; }

    [Parameter(Mandatory = false, Position = 1)]
    public double? StalenessMultiplier { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<SettingsHttpService>();
        var request = new UpdateServerSettingsRequest
        {
            CertificateRotationInterval = CertificateRotationInterval,
            StalenessMultiplier = StalenessMultiplier,
        };

        var result = service.UpdateServerSettingsAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
