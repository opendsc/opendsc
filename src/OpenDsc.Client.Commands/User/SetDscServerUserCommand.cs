// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.User;

[Cmdlet(VerbsCommon.Set, "DscServerUser", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(UserSummary))]
public sealed class SetDscServerUserCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid UserId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Username { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 2)]
    [ValidateNotNullOrEmpty]
    public string Email { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 3)]
    public bool IsActive { get; set; }

    [Parameter(Mandatory = true, Position = 4)]
    public bool RequirePasswordChange { get; set; }

    [Parameter(Mandatory = true, Position = 5)]
    public AccountType AccountType { get; set; }

    [Parameter(Mandatory = false, Position = 6)]
    public string? Description { get; set; }

    [Parameter(Mandatory = true, Position = 7)]
    public bool IsLocked { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(UserId.ToString())) return;
        var service = GetRequiredService<UserHttpService>();
        var request = new UpdateUserRequest
        {
            Username = Username,
            Email = Email,
            IsActive = IsActive,
            RequirePasswordChange = RequirePasswordChange,
            AccountType = AccountType,
            Description = Description,
            IsLocked = IsLocked,
        };

        var result = service.UpdateUserAsync(UserId, request, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
