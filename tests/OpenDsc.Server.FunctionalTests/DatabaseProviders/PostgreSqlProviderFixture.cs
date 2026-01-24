// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Testcontainers.PostgreSql;

namespace OpenDsc.Server.FunctionalTests.DatabaseProviders;

public sealed class PostgreSqlProviderFixture : DatabaseProviderFixture
{
    private PostgreSqlContainer? _container;

    public override string ProviderName => "PostgreSQL";

    public override async Task InitializeAsync()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:17-alpine")
            .Build();

        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    protected override async Task CleanupAsync()
    {
        if (_container is not null)
        {
            await _container.StopAsync();
            await _container.DisposeAsync();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _container is not null)
        {
            _container.StopAsync().GetAwaiter().GetResult();
            _container.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.Dispose(disposing);
    }
}
