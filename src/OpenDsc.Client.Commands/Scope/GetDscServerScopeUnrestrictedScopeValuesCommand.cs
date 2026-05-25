// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Scope;

[Cmdlet("Get", "DscServerScopeUnrestrictedScopeValues")]
[OutputType(typeof(string))]
public sealed class GetDscServerScopeUnrestrictedScopeValuesCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ScopeTypeId { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ScopeHttpService>();
        var result = service.GetUnrestrictedScopeValuesAsync(ScopeTypeId, CancellationToken.None).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
