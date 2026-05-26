// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Net;
using System.Security;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.User;

[Cmdlet(VerbsCommon.Set, "DscServerUserPassword", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
public sealed class SetDscServerUserPasswordCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid UserId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public SecureString CurrentPassword { get; set; } = null!;

    [Parameter(Mandatory = true, Position = 2)]
    public SecureString NewPassword { get; set; } = null!;

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(UserId.ToString())) return;
        var service = GetRequiredService<UserHttpService>();
        var request = new ChangePasswordRequest
        {
            CurrentPassword = new NetworkCredential(string.Empty, CurrentPassword).Password,
            NewPassword = new NetworkCredential(string.Empty, NewPassword).Password,
        };

        service.ChangePasswordAsync(UserId, request, PipelineStopToken).GetAwaiter().GetResult();
    }
}
