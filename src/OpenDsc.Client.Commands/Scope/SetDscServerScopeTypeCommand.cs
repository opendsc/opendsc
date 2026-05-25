// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.Scope;

[Cmdlet("Set", "DscServerScopeType")]
[OutputType(typeof(ScopeTypeDetails))]
public sealed class SetDscServerScopeTypeCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid Id { get; set; }

    [Parameter(Mandatory = false, Position = 1)]
    public string? Description { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ScopeHttpService>();
        var request = new UpdateScopeTypeRequest
        {
            Description = Description,
        };

        var result = service.UpdateScopeTypeAsync(Id, request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
