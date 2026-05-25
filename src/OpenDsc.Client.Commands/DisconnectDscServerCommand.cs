// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

namespace OpenDsc.Client.Commands;

[Cmdlet(VerbsCommunications.Disconnect, "DscServer")]
[OutputType(typeof(bool))]
public sealed class DisconnectDscServerCommand : PSCmdlet
{
    protected override void EndProcessing()
    {
        WriteObject(DscClientSession.Disconnect());
    }
}
