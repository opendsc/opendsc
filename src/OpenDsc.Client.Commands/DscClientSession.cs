// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.Extensions.DependencyInjection;

using OpenDsc.Client.Authentication;

namespace OpenDsc.Client.Commands;

public static class DscClientSession
{
    private static readonly object SyncRoot = new();
    private static ServiceProvider? _provider;
    private static DscClientSessionState? _state;

    public static DscClientSessionState Connect(Uri serverUri, string token, TimeSpan? timeout)
    {
        var services = new ServiceCollection();
        services.AddOpenDscClient(options =>
        {
            options.BaseAddress = serverUri;
            options.Credential = new ApiKeyCredential(token);
            options.Timeout = timeout;
        });

        var provider = services.BuildServiceProvider();
        var state = new DscClientSessionState
        {
            ServerUri = serverUri,
            ConnectedAt = DateTimeOffset.UtcNow,
        };

        lock (SyncRoot)
        {
            _provider?.Dispose();
            _provider = provider;
            _state = state;
        }

        return state;
    }

    public static bool Disconnect()
    {
        lock (SyncRoot)
        {
            if (_provider is null)
            {
                return false;
            }

            _provider.Dispose();
            _provider = null;
            _state = null;
            return true;
        }
    }

    public static DscClientSessionState GetState()
    {
        lock (SyncRoot)
        {
            return _state ?? throw new InvalidOperationException(
                "No OpenDSC server connection is active. Run Connect-DscServer first.");
        }
    }

    public static T GetRequiredService<T>() where T : notnull
    {
        lock (SyncRoot)
        {
            return (_provider ?? throw new InvalidOperationException(
                "No OpenDSC server connection is active. Run Connect-DscServer first."))
                .GetRequiredService<T>();
        }
    }

    public static object GetRequiredService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        lock (SyncRoot)
        {
            return (_provider ?? throw new InvalidOperationException(
                "No OpenDSC server connection is active. Run Connect-DscServer first."))
                .GetRequiredService(serviceType);
        }
    }
}
