// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Xunit;

namespace OpenDsc.Server.FunctionalTests.DatabaseProviders;

[CollectionDefinition("SQLite")]
public class SqliteCollection : ICollectionFixture<SqliteProviderFixture>
{
}

[CollectionDefinition("SQL Server")]
public class SqlServerCollection : ICollectionFixture<SqlServerProviderFixture>
{
}

[CollectionDefinition("PostgreSQL")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlProviderFixture>
{
}
