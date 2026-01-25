// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Testcontainers.MsSql;

namespace OpenDsc.Server.FunctionalTests.DatabaseProviders;

public sealed class SqlServerProviderFixture : DatabaseProviderFixture
{
    private MsSqlContainer? _container;

    public override string ProviderName => "SqlServer";

    public override async Task InitializeAsync()
    {
        _container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
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
}
