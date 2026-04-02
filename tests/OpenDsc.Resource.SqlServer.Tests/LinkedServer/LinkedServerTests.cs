// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using LinkedServerResource = OpenDsc.Resource.SqlServer.LinkedServer.Resource;
using LinkedServerSchema = OpenDsc.Resource.SqlServer.LinkedServer.Schema;

using Xunit;

namespace OpenDsc.Resource.SqlServer.Tests.LinkedServer;

[Trait("Category", "Integration")]
public sealed class LinkedServerTests : SqlServerTestBase
{
    private const string Prefix = "OpenDscTest_LS_";
    private readonly LinkedServerResource _resource = new(SourceGenerationContext.Default);

    public LinkedServerTests(SqlServerFixture fixture) : base(fixture) { }

    private LinkedServerSchema NewSchema(string name) => new()
    {
        ServerInstance = ServerInstance,
        ConnectUsername = ConnectUsername,
        ConnectPassword = ConnectPassword,
        Name = name
    };

    [Fact]
    public void Get_NonExistentLinkedServer_ReturnsExistFalse()
    {
        var result = _resource.Get(NewSchema("NonExistentLinkedServer_12345_XYZ"));
        result.Exist.Should().BeFalse();
        result.Name.Should().Be("NonExistentLinkedServer_12345_XYZ");
    }

    [Fact]
    public void Set_CreateLinkedServer_LinkedServerExists()
    {
        var name = $"{Prefix}Create1";
        try
        {
            var schema = NewSchema(name);
            schema.ProductName = "SQL Server";
            schema.ProviderName = "SQLNCLI11";
            schema.DataSource = ServerInstance;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.Name.Should().Be(name);
            result.ProductName.Should().Be("SQL Server");
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            ExecuteSqlSafe($"EXEC sp_dropserver @server = N'{name}', @droplogins='droplogins'");
        }
    }

    [Fact]
    public void Set_UpdateLinkedServerProperties_PropertiesUpdated()
    {
        var name = $"{Prefix}Update1";
        try
        {
            var create = NewSchema(name);
            create.ProductName = "SQL Server";
            create.ProviderName = "SQLNCLI11";
            create.DataSource = ServerInstance;
            _resource.Set(create);

            var update = NewSchema(name);
            update.RpcOut = true;
            update.DataAccess = true;
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.RpcOut.Should().BeTrue();
            result.DataAccess.Should().BeTrue();
        }
        finally
        {
            ExecuteSqlSafe($"EXEC sp_dropserver @server = N'{name}', @droplogins='droplogins'");
        }
    }

    [Fact]
    public void Set_SetConnectTimeout_TimeoutSet()
    {
        var name = $"{Prefix}Timeout1";
        try
        {
            var schema = NewSchema(name);
            schema.ProductName = "SQL Server";
            schema.ProviderName = "SQLNCLI11";
            schema.DataSource = ServerInstance;
            schema.ConnectTimeout = 30;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.ConnectTimeout.Should().Be(30);
        }
        finally
        {
            ExecuteSqlSafe($"EXEC sp_dropserver @server = N'{name}', @droplogins='droplogins'");
        }
    }

    [Fact]
    public void Delete_ExistingLinkedServer_LinkedServerGone()
    {
        var name = $"{Prefix}Delete1";
        var create = NewSchema(name);
        create.ProductName = "SQL Server";
        create.ProviderName = "SQLNCLI11";
        create.DataSource = ServerInstance;
        _resource.Set(create);

        _resource.Delete(NewSchema(name));

        var result = _resource.Get(NewSchema(name));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Delete_NonExistentLinkedServer_DoesNotThrow()
    {
        var act = () => _resource.Delete(NewSchema("NonExistentLinkedServer_ToDelete_XYZ"));
        act.Should().NotThrow();
    }

    [Fact]
    public void Export_ReturnsLinkedServers()
    {
        var name = $"{Prefix}Export1";
        try
        {
            var schema = NewSchema(name);
            schema.ProductName = "SQL Server";
            schema.ProviderName = "SQLNCLI11";
            schema.DataSource = ServerInstance;
            _resource.Set(schema);

            var filter = new LinkedServerSchema
            {
                ServerInstance = ServerInstance,
                ConnectUsername = ConnectUsername,
                ConnectPassword = ConnectPassword,
                Name = string.Empty
            };
            var results = _resource.Export(filter).ToList();
            results.Should().NotBeEmpty();
            results.Select(r => r.Name).Should().Contain(name);
        }
        finally
        {
            ExecuteSqlSafe($"EXEC sp_dropserver @server = N'{name}', @droplogins='droplogins'");
        }
    }
}
