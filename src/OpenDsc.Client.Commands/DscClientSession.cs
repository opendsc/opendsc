// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.Extensions.DependencyInjection;

using OpenDsc.Client.Authentication;

namespace OpenDsc.Client.Commands;

public static class DscClientSession
{
    private static readonly object SyncRoot = new();
    private static DscServerSession? _default;

    internal static DscServerSession Create(Uri serverUri, string token, TimeSpan? timeout)
    {
        var services = new ServiceCollection();
        services.AddOpenDscClient(options =>
        {
            options.BaseAddress = serverUri;
            options.Credential = new ApiKeyCredential(token);
            options.Timeout = timeout;
        });

        return new DscServerSession(serverUri, services.BuildServiceProvider());
    }

    internal static DscServerSession? GetDefault()
    {
        lock (SyncRoot)
        {
            return _default;
        }
    }

    internal static void SetDefault(DscServerSession session)
    {
        lock (SyncRoot)
        {
            _default = session;
        }
    }

    internal static bool ClearDefault(DscServerSession? session)
    {
        lock (SyncRoot)
        {
            if (session is null)
            {
                if (_default is null)
                {
                    return false;
                }

                _default.Dispose();
                _default = null;
                return true;
            }

            if (!ReferenceEquals(_default, session))
            {
                return false;
            }

            _default = null;
            return true;
        }
    }
}
