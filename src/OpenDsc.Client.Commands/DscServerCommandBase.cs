// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

namespace OpenDsc.Client.Commands;

public abstract class DscServerCommandBase : PSCmdlet
{
    protected DscClientSessionState ConnectionState => DscClientSession.GetState();

    protected T GetRequiredService<T>() where T : notnull => DscClientSession.GetRequiredService<T>();

    protected object GetRequiredService(Type serviceType) => DscClientSession.GetRequiredService(serviceType);

    protected override void BeginProcessing()
    {
        try
        {
            _ = ConnectionState;
        }
        catch (InvalidOperationException ex)
        {
            ThrowTerminatingError(new ErrorRecord(ex, "OpenDscClient.NotConnected", ErrorCategory.ConnectionError, null));
        }
    }
}
