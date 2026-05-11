// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using OpenDsc.Contracts.Settings;
using OpenDsc.Server.Data;

namespace OpenDsc.Server.Services;

public sealed class HealthService(ServerDbContext db) : IHealthService
{
    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        return db.Database.CanConnectAsync(cancellationToken);
    }
}
