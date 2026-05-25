// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Settings;

namespace OpenDsc.Client.Commands.Scope;

[Cmdlet("Get", "DscServerScopeParameters")]
[OutputType(typeof(ScopeParameterInfo))]
public sealed class GetDscServerScopeParametersCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid SchemaId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public Guid ScopeTypeId { get; set; }

    [Parameter(Mandatory = true, Position = 2)]
    [ValidateNotNullOrEmpty]
    public string ScopeValue { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ScopeHttpService>();
        var result = service.GetScopeParametersAsync(SchemaId, ScopeTypeId, ScopeValue, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
