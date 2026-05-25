// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;
using OpenDsc.Contracts.Parameters;

namespace OpenDsc.Client.Commands.Parameter;

[Cmdlet("Set", "DscServerParameter")]
public sealed class SetDscServerParameterCommand : DscServerCommandBase
{
    [Parameter(Mandatory = true, Position = 0)]
    public Guid ParameterId { get; set; }

    [Parameter(Mandatory = true, Position = 1)]
    [ValidateNotNullOrEmpty]
    public string Content { get; set; } = string.Empty;

    protected override void ProcessRecord()
    {
        var service = GetRequiredService<ParameterHttpService>();
        var request = new UpdateParameterRequest
        {
            Content = Content,
        };

        service.UpdateAsync(ParameterId, request, CancellationToken.None).GetAwaiter().GetResult();
    }
}
