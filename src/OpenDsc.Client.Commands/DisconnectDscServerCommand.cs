// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

namespace OpenDsc.Client.Commands;

[Cmdlet(VerbsCommon.Remove, "DscServerSession", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(bool))]
public sealed class RemoveDscServerSessionCommand : PSCmdlet
{
    [Parameter(Position = 0, ValueFromPipeline = true)]
    public DscServerSession? Session { get; set; }

    protected override void ProcessRecord()
    {
        var target = Session?.ServerUri.ToString() ?? "default session";

        if (!ShouldProcess(target, "Disconnect"))
        {
            return;
        }

        if (Session is not null)
        {
            var wasDefault = DscClientSession.ClearDefault(Session);
            Session.Dispose();
            WriteObject(wasDefault);
        }
        else
        {
            WriteObject(DscClientSession.ClearDefault(null));
        }
    }
}
