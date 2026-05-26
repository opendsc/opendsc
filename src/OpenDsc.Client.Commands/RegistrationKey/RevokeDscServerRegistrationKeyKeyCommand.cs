// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.RegistrationKey;

[Cmdlet(VerbsSecurity.Revoke, "DscServerRegistrationKeyKey", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public sealed class RevokeDscServerRegistrationKeyKeyCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid Id { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(Id.ToString())) return;
        var service = GetRequiredService<RegistrationKeyHttpService>();
        service.RevokeKeyAsync(Id, PipelineStopToken).GetAwaiter().GetResult();
    }
}
