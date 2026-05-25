// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.RegistrationKey;

[Cmdlet("Update", "DscServerRegistrationKeyKey")]
[OutputType(typeof(RegistrationKeyResponse))]
public sealed class UpdateDscServerRegistrationKeyKeyCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<RegistrationKeyHttpService>();
        var result = service.RotateKeyAsync(CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
