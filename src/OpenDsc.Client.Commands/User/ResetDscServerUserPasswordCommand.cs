// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.User;

[Cmdlet("Reset", "DscServerUserPassword")]
public sealed class ResetDscServerUserPasswordCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid UserId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string NewPassword { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<UserHttpService>();
        var request = new ResetPasswordRequest
        {
            NewPassword = NewPassword,
        };

        service.ResetPasswordAsync(UserId, request, CancellationToken.None).GetAwaiter().GetResult();
    }
}
