// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Net;
using System.Security;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.User;

[Cmdlet(VerbsCommon.New, "DscServerUser", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(UserSummary))]
public sealed class NewDscServerUserCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Username { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Email { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 2)]
    public SecureString Password { get; set; } = null!;

    [Parameter(Mandatory = true, Position = 3)]
    public AccountType AccountType { get; set; }

    [Parameter(Mandatory = true, Position = 4)]
    public bool RequirePasswordChange { get; set; }

    [Parameter(Mandatory = false, Position = 5)]
    public string? Description { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(Username)) return;
        var service = GetRequiredService<UserHttpService>();
        var request = new CreateUserRequest
        {
            Username = Username,
            Email = Email,
            Password = new NetworkCredential(string.Empty, Password).Password,
            AccountType = AccountType,
            RequirePasswordChange = RequirePasswordChange,
            Description = Description,
        };

        var result = service.CreateUserAsync(request, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
