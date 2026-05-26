// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.RegistrationKey;

[Cmdlet(VerbsCommon.New, "DscServerRegistrationKeyKey", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(RegistrationKeyResponse))]
public sealed class NewDscServerRegistrationKeyKeyCommand : DscServerCommandBase
{
    [Parameter(Mandatory = false, Position = 0)]
    public DateTimeOffset? ExpiresAt { get; set; }

    [Parameter(Mandatory = false, Position = 1)]
    public int? MaxUses { get; set; }

    [Parameter(Mandatory = false, Position = 2)]
    public string? Description { get; set; }

    protected override void ProcessRecord()
    {
        if (!ShouldProcess(string.Empty)) return;
        var service = GetRequiredService<RegistrationKeyHttpService>();
        var request = new CreateRegistrationKeyRequest
        {
            ExpiresAt = ExpiresAt,
            MaxUses = MaxUses,
            Description = Description,
        };

        var result = service.CreateKeyAsync(request, PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
