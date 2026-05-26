// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace OpenDsc.Client.Commands;

public sealed class DscServerSession : IDisposable
{
    private readonly ServiceProvider _provider;
    private bool _disposed;

    public Uri ServerUri { get; }

    public DateTimeOffset ConnectedAt { get; }

    internal DscServerSession(Uri serverUri, ServiceProvider provider)
    {
        ServerUri = serverUri;
        ConnectedAt = DateTimeOffset.UtcNow;
        _provider = provider;
    }

    internal T GetRequiredService<T>() where T : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _provider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _provider.Dispose();
    }
}
