// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.RegistrationKey;

[Cmdlet("Revoke", "DscServerRegistrationKeyKey")]
public sealed class RevokeDscServerRegistrationKeyKeyCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid Id { get; set; } = default;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<RegistrationKeyHttpService>();
        service.RevokeKeyAsync(Id, CancellationToken.None).GetAwaiter().GetResult();
    }
}
