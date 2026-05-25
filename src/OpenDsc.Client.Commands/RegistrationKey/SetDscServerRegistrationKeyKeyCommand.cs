// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.RegistrationKey;

[Cmdlet("Set", "DscServerRegistrationKeyKey")]
[OutputType(typeof(RegistrationKeyResponse))]
public sealed class SetDscServerRegistrationKeyKeyCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid Id { get; set; }

    [Parameter(Mandatory = false, Position = 1)]
    public string? Description { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<RegistrationKeyHttpService>();
        var request = new UpdateRegistrationKeyRequest
        {
            Description = Description,
        };

        var result = service.UpdateKeyAsync(Id, request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
