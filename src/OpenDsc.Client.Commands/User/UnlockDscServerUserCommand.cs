// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.User;

[Cmdlet("Unlock", "DscServerUser")]
public sealed class UnlockDscServerUserCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid UserId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<UserHttpService>();
        service.UnlockUserAsync(UserId, CancellationToken.None).GetAwaiter().GetResult();
    }
}
