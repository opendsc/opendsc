// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.SqlServer.Management.Smo;

using Xunit;

namespace OpenDsc.Resource.SqlServer.Tests;

public sealed class SqlServerFixture : IDisposable
{
    public bool IsAvailable { get; }
    public string ServerInstance { get; }
    public string? Username { get; }
    public string? Password { get; }

    public SqlServerFixture()
    {
        ServerInstance = Environment.GetEnvironmentVariable("SQLSERVER_INSTANCE")
            ?? (OperatingSystem.IsLinux() ? "localhost" : ".");

        var saPassword = Environment.GetEnvironmentVariable("SQLSERVER_SA_PASSWORD");
        Username = saPassword is not null ? (Environment.GetEnvironmentVariable("SQLSERVER_USERNAME") ?? "sa") : null;
        Password = saPassword;

        try
        {
            var server = SqlConnectionHelper.CreateConnection(ServerInstance, Username, Password);
            server.ConnectionContext.ConnectTimeout = 10;
            server.ConnectionContext.Connect();
            SqlConnectionHelper.SafeDisconnect(server);
            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
        }
    }

    public void Dispose() { }
}

public abstract class SqlServerTestBase : IClassFixture<SqlServerFixture>
{
    protected readonly string ServerInstance;
    protected readonly string? ConnectUsername;
    protected readonly string? ConnectPassword;

    protected SqlServerTestBase(SqlServerFixture fixture)
    {
        Assert.SkipUnless(fixture.IsAvailable, "SQL Server not available");
        ServerInstance = fixture.ServerInstance;
        ConnectUsername = fixture.Username;
        ConnectPassword = fixture.Password;
    }

    protected Schema CreateSchema<Schema>(Action<Schema> configure) where Schema : new()
    {
        var schema = new Schema();
        configure(schema);
        return schema;
    }

    protected void ExecuteSql(string sql, string? database = null)
    {
        var server = SqlConnectionHelper.CreateConnection(ServerInstance, ConnectUsername, ConnectPassword);
        try
        {
            if (database is not null)
            {
                server.ConnectionContext.Connect();
                server.ConnectionContext.ExecuteNonQuery($"USE [{database}]");
            }

            server.ConnectionContext.ExecuteNonQuery(sql);
        }
        finally
        {
            SqlConnectionHelper.SafeDisconnect(server);
        }
    }

    protected void ExecuteSqlSafe(string sql, string? database = null)
    {
        try
        {
            ExecuteSql(sql, database);
        }
        catch
        {
            // Ignore errors in cleanup/helper operations
        }
    }

    protected Server ConnectToServer() =>
        SqlConnectionHelper.CreateConnection(ServerInstance, ConnectUsername, ConnectPassword);
}
