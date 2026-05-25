// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.Scope;

[Cmdlet("New", "DscServerScopeType")]
[OutputType(typeof(ScopeTypeDetails))]
public sealed class NewDscServerScopeTypeCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    [ValidateNotNullOrEmpty]
    public string Name { get; set; } = string.Empty;

    [Parameter(Mandatory = false, Position = 1)]
    public string? Description { get; set; }

    [Parameter(Mandatory = false, Position = 2)]
    public ScopeValueMode? ValueMode { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ScopeHttpService>();
        var request = new CreateScopeTypeRequest
        {
            Name = Name,
            Description = Description,
            ValueMode = ValueMode,
        };

        var result = service.CreateScopeTypeAsync(request, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
