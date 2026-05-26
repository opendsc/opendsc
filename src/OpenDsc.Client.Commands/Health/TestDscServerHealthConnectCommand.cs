// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

using OpenDsc.Client.Services;

namespace OpenDsc.Client.Commands.Health;

[Cmdlet(VerbsDiagnostic.Test, "DscServerHealthConnect")]
[OutputType(typeof(bool))]
public sealed class TestDscServerHealthConnectCommand : DscServerCommandBase
{
    protected override void ProcessRecord()
    {
        var service = GetRequiredService<HealthHttpService>();
        var result = service.CanConnectAsync(PipelineStopToken).GetAwaiter().GetResult();
        WriteObject(result, enumerateCollection: true);
    }
}
