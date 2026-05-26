// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Net;
using System.Security;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Users;

namespace OpenDsc.Client.Commands.User;

[Cmdlet(VerbsDiagnostic.Test, "DscServerUser")]
[OutputType(typeof(AuthenticationResult))]
public sealed class TestDscServerUserCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Username { get; set; } = string.Empty;

    [Parameter(Mandatory = true, Position = 1)]
    public SecureString Password { get; set; } = null!;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<UserHttpService>();
        var result = service.AuthenticateAsync(Username, new NetworkCredential(string.Empty, Password).Password, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
