// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Settings;

/// <summary>
/// Read operations for server health and connectivity.
/// </summary>
public interface IHealthReader
{
    /// <summary>
    /// Checks whether the server can connect to its backing data store.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns><see langword="true"/> if the connection is available; otherwise <see langword="false"/>.</returns>
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Umbrella service interface for all health operations.
/// Implements all capability sub-interfaces; register via this umbrella in DI.
/// </summary>
public interface IHealthService : IHealthReader
{
}
