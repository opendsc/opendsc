// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

namespace OpenDsc.Client.Commands;

[Cmdlet(VerbsCommunications.Connect, "DscServer")]
[OutputType(typeof(DscClientSessionState))]
public sealed class ConnectDscServerCommand : PSCmdlet
{
    [Parameter(Mandatory = true)]
    [Alias("Uri")]
    public Uri ServerUri { get; set; } = null!;

    [Parameter(Mandatory = true)]
    [ValidateNotNullOrEmpty]
    public string Token { get; set; } = string.Empty;

    [Parameter]
    [ValidateRange(1, 3600)]
    public int? TimeoutSeconds { get; set; }

    protected override void EndProcessing()
    {
        TimeSpan? timeout = TimeoutSeconds is null
            ? null
            : TimeSpan.FromSeconds(TimeoutSeconds.Value);

        var state = DscClientSession.Connect(ServerUri, Token, timeout);
        WriteObject(state);
    }
}
