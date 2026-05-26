// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.RegistrationKey;

[Cmdlet(VerbsData.Update, "DscServerRegistrationKeyKey", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(RegistrationKeyResponse))]
public sealed class UpdateDscServerRegistrationKeyKeyCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        if (!ShouldProcess(string.Empty)) return;
        var service = GetRequiredService<RegistrationKeyHttpService>();
        var result = service.RotateKeyAsync(PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
