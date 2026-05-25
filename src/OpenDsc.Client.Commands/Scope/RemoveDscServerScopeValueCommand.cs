// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Scope;

[Cmdlet("Remove", "DscServerScopeValue")]
public sealed class RemoveDscServerScopeValueCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ScopeTypeId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    public Guid Id { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ScopeHttpService>();
        service.DeleteScopeValueAsync(ScopeTypeId, Id, CancellationToken.None).GetAwaiter().GetResult();
    }
}
