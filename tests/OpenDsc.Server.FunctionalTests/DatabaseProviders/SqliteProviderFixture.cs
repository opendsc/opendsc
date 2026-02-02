// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.Data.Sqlite;

namespace OpenDsc.Server.FunctionalTests.DatabaseProviders;

public sealed class SqliteProviderFixture : DatabaseProviderFixture
{
    private SqliteConnection? _connection;

    public override string ProviderName => "SQLite";

    public override async Task InitializeAsync()
    {
        var uniqueDbName = $"TestDb_{Guid.NewGuid():N}";
        var connectionString = $"DataSource={uniqueDbName};Mode=Memory;Cache=Shared";
        _connection = new SqliteConnection(connectionString);
        await _connection.OpenAsync();
        ConnectionString = connectionString;
    }

    protected override Task CleanupAsync()
    {
        _connection?.Close();
        _connection?.Dispose();
        return Task.CompletedTask;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Close();
            _connection?.Dispose();
        }

        base.Dispose(disposing);
    }
}
