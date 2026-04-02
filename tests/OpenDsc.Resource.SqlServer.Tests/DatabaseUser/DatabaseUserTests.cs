// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using DatabaseUserResource = OpenDsc.Resource.SqlServer.DatabaseUser.Resource;
using DatabaseUserSchema = OpenDsc.Resource.SqlServer.DatabaseUser.Schema;
using UserType = Microsoft.SqlServer.Management.Smo.UserType;

using Xunit;

namespace OpenDsc.Resource.SqlServer.Tests.DatabaseUser;

[Trait("Category", "Integration")]
public sealed class DatabaseUserTests : SqlServerTestBase
{
    private const string Prefix = "OpenDscTest_DbUser_";
    private const string TestDb = "OpenDscTest_DbUser_DB";
    private const string TestLogin1 = "OpenDscTest_DbUser_Login1";
    private const string TestLogin2 = "OpenDscTest_DbUser_Login2";
    private readonly DatabaseUserResource _resource = new(SourceGenerationContext.Default);

    public DatabaseUserTests(SqlServerFixture fixture) : base(fixture)
    {
        ExecuteSqlSafe($"IF DB_ID('{TestDb}') IS NULL CREATE DATABASE [{TestDb}]");
        ExecuteSqlSafe($"IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '{TestLogin1}') CREATE LOGIN [{TestLogin1}] WITH PASSWORD = 'T3stP@ssw0rd!User123', CHECK_POLICY = OFF");
        ExecuteSqlSafe($"IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '{TestLogin2}') CREATE LOGIN [{TestLogin2}] WITH PASSWORD = 'T3stP@ssw0rd!User456', CHECK_POLICY = OFF");
        ExecuteSqlSafe($"USE [{TestDb}]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '{TestLogin1}') CREATE USER [{TestLogin1}] FOR LOGIN [{TestLogin1}]");
    }

    private DatabaseUserSchema NewSchema(string name, string? db = null) => new()
    {
        ServerInstance = ServerInstance,
        ConnectUsername = ConnectUsername,
        ConnectPassword = ConnectPassword,
        DatabaseName = db ?? TestDb,
        Name = name
    };

    [Fact]
    public void Get_NonExistentUser_ReturnsExistFalse()
    {
        var result = _resource.Get(NewSchema("NonExistentUser_12345_XYZ"));
        result.Exist.Should().BeFalse();
        result.Name.Should().Be("NonExistentUser_12345_XYZ");
    }

    [Fact]
    public void Get_DboUser_ReturnsProperties()
    {
        var result = _resource.Get(NewSchema("dbo"));
        result.Name.Should().Be("dbo");
        result.Exist.Should().NotBe(false);
    }

    [Fact]
    public void Get_NonExistentDatabase_ReturnsExistFalse()
    {
        var result = _resource.Get(NewSchema("SomeUser", "NonExistentDb_XYZ"));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Set_CreateUserMappedToLogin_UserExists()
    {
        var name = $"{Prefix}Create1";
        try
        {
            var schema = NewSchema(name);
            schema.Login = TestLogin2;
            schema.UserType = UserType.SqlUser;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.Name.Should().Be(name);
            result.Login.Should().Be(TestLogin2);
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; DROP USER IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_CreateNoLoginUser_UserExists()
    {
        var name = $"{Prefix}NoLogin1";
        try
        {
            var schema = NewSchema(name);
            schema.UserType = UserType.NoLogin;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.Name.Should().Be(name);
            result.UserType.Should().Be(UserType.NoLogin);
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; DROP USER IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_UpdateDefaultSchema_SchemaUpdated()
    {
        var name = $"{Prefix}Schema1";
        try
        {
            var create = NewSchema(name);
            create.UserType = UserType.NoLogin;
            _resource.Set(create);

            var update = NewSchema(name);
            update.DefaultSchema = "dbo";
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.DefaultSchema.Should().Be("dbo");
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; DROP USER IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_Idempotent_NoError()
    {
        var name = $"{Prefix}Idempotent1";
        try
        {
            var schema = NewSchema(name);
            schema.UserType = UserType.NoLogin;
            _resource.Set(schema);
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; DROP USER IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Delete_ExistingUser_UserGone()
    {
        var name = $"{Prefix}Delete1";
        var create = NewSchema(name);
        create.UserType = UserType.NoLogin;
        _resource.Set(create);

        var del = NewSchema(name);
        del.Exist = false;
        _resource.Delete(del);

        var result = _resource.Get(NewSchema(name));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Delete_NonExistentUser_DoesNotThrow()
    {
        var schema = NewSchema("NonExistentUser_ToDelete_XYZ");
        schema.Exist = false;
        var act = () => _resource.Delete(schema);
        act.Should().NotThrow();
    }

    [Fact]
    public void Delete_SystemUser_Throws()
    {
        var schema = NewSchema("dbo");
        schema.Exist = false;
        var act = () => _resource.Delete(schema);
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void Export_ReturnsUsersInDatabase()
    {
        var filter = new DatabaseUserSchema
        {
            ServerInstance = ServerInstance,
            ConnectUsername = ConnectUsername,
            ConnectPassword = ConnectPassword,
            DatabaseName = TestDb,
            Name = string.Empty
        };

        var results = _resource.Export(filter).ToList();
        results.Should().NotBeEmpty();
        results.Select(r => r.DatabaseName).Should().AllBe(TestDb);
    }
}
