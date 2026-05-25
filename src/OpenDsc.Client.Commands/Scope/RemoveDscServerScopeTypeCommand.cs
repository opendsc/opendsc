// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Scope;

[Cmdlet("Remove", "DscServerScopeType")]
public sealed class RemoveDscServerScopeTypeCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid Id { get; set; }

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ScopeHttpService>();
        service.DeleteScopeTypeAsync(Id, CancellationToken.None).GetAwaiter().GetResult();
    }
}
