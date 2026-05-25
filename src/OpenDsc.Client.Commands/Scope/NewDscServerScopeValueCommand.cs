// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.Scope;

[Cmdlet("New", "DscServerScopeValue")]
[OutputType(typeof(ScopeValueDetails))]
public sealed class NewDscServerScopeValueCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ScopeTypeId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Value { get; set; } = string.Empty;

    [Parameter(Mandatory = false, Position = 2)]
    public string? Description { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ScopeHttpService>();
        var request = new CreateScopeValueRequest
        {
            Value = Value,
            Description = Description,
        };

        var result = service.CreateScopeValueAsync(ScopeTypeId, request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
