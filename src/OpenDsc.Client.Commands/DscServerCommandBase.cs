// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

namespace OpenDsc.Client.Commands;

public abstract class DscServerCommandBase : PSCmdlet
{
    [Parameter]
    public DscServerSession? Session { get; set; }

    private DscServerSession ActiveSession =>
        Session ?? DscClientSession.GetDefault()
            ?? throw new InvalidOperationException(
                "No DSC server session is active. Run New-DscServerSession first or specify -Session.");

    protected T GetRequiredService<T>() where T : notnull => ActiveSession.GetRequiredService<T>();

    protected override void BeginProcessing()
    {
        if (Session is null && DscClientSession.GetDefault() is null)
        {
            ThrowTerminatingError(new ErrorRecord(
                new InvalidOperationException(
                    "No DSC server session is active. Run New-DscServerSession first or specify -Session."),
                "OpenDscClient.NoSession",
                ErrorCategory.ConnectionError,
                null));
        }
    }
}
