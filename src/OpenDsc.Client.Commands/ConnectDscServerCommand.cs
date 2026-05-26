// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;
using System.Net;
using System.Security;

namespace OpenDsc.Client.Commands;

[Cmdlet(VerbsCommon.New, "DscServerSession")]
[OutputType(typeof(DscServerSession))]
public sealed class NewDscServerSessionCommand : PSCmdlet
{
    [Parameter(Mandatory = true, Position = 0)]
    [Alias("Uri")]
    public Uri ServerUri { get; set; } = null!;

    [Parameter(Mandatory = true, Position = 1)]
    public SecureString Token { get; set; } = null!;

    [Parameter]
    [ValidateRange(1, 3600)]
    public int? TimeoutSeconds { get; set; }

    protected override void EndProcessing()
    {
        var token = new NetworkCredential(string.Empty, Token).Password;

        TimeSpan? timeout = TimeoutSeconds is null
            ? null
            : TimeSpan.FromSeconds(TimeoutSeconds.Value);

        var session = DscClientSession.Create(ServerUri, token, timeout);
        DscClientSession.SetDefault(session);
        WriteObject(session);
    }
}
