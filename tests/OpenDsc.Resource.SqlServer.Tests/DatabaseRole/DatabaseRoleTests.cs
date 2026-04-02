// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using DatabaseRoleResource = OpenDsc.Resource.SqlServer.DatabaseRole.Resource;
using DatabaseRoleSchema = OpenDsc.Resource.SqlServer.DatabaseRole.Schema;

using Xunit;

namespace OpenDsc.Resource.SqlServer.Tests.DatabaseRole;

[Trait("Category", "Integration")]
public sealed class DatabaseRoleTests : SqlServerTestBase
{
    private const string Prefix = "OpenDscTest_DbRole_";
    private const string TestDb = "OpenDscTest_DbRole_DB";
    private readonly DatabaseRoleResource _resource = new(SourceGenerationContext.Default);

    public DatabaseRoleTests(SqlServerFixture fixture) : base(fixture)
    {
        ExecuteSqlSafe($"IF DB_ID('{TestDb}') IS NULL CREATE DATABASE [{TestDb}]");
        ExecuteSqlSafe($"USE [{TestDb}]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'TestUser1') CREATE USER [TestUser1] WITHOUT LOGIN");
        ExecuteSqlSafe($"USE [{TestDb}]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'TestUser2') CREATE USER [TestUser2] WITHOUT LOGIN");
        ExecuteSqlSafe($"USE [{TestDb}]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = 'TestUser3') CREATE USER [TestUser3] WITHOUT LOGIN");
    }

    private DatabaseRoleSchema NewSchema(string name, string? db = null) => new()
    {
        ServerInstance = ServerInstance,
        ConnectUsername = ConnectUsername,
        ConnectPassword = ConnectPassword,
        DatabaseName = db ?? TestDb,
        Name = name
    };

    [Fact]
    public void Get_NonExistentRole_ReturnsExistFalse()
    {
        var result = _resource.Get(NewSchema("NonExistentRole_12345_XYZ"));
        result.Exist.Should().BeFalse();
        result.Name.Should().Be("NonExistentRole_12345_XYZ");
    }

    [Fact]
    public void Get_FixedRole_ReturnsIsFixedRole()
    {
        var result = _resource.Get(NewSchema("db_owner"));
        result.Name.Should().Be("db_owner");
        result.IsFixedRole.Should().BeTrue();
        result.Exist.Should().NotBe(false);
    }

    [Fact]
    public void Set_CreateRole_RoleExists()
    {
        var name = $"{Prefix}Create1";
        try
        {
            _resource.Set(NewSchema(name));

            var result = _resource.Get(NewSchema(name));
            result.Name.Should().Be(name);
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; DROP ROLE IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_CreateRoleWithMembers_MembersAssigned()
    {
        var name = $"{Prefix}Members1";
        try
        {
            var schema = NewSchema(name);
            schema.Members = ["TestUser1", "TestUser2"];
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.Members.Should().Contain("TestUser1");
            result.Members.Should().Contain("TestUser2");
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; DROP ROLE IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_ChangeRoleOwner_OwnerUpdated()
    {
        var name = $"{Prefix}Owner1";
        try
        {
            _resource.Set(NewSchema(name));

            var update = NewSchema(name);
            update.Owner = "dbo";
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.Owner.Should().Be("dbo");
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; DROP ROLE IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_AddMembersAdditive_AllMembersPresent()
    {
        var name = $"{Prefix}MembersAdd1";
        try
        {
            var create = NewSchema(name);
            create.Members = ["TestUser1"];
            _resource.Set(create);

            var update = NewSchema(name);
            update.Members = ["TestUser2"];
            update.Purge = false;
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.Members.Should().Contain("TestUser1");
            result.Members.Should().Contain("TestUser2");
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; DROP ROLE IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_PurgeMembers_OnlyNewMembersPresent()
    {
        var name = $"{Prefix}MembersPurge1";
        try
        {
            var create = NewSchema(name);
            create.Members = ["TestUser1", "TestUser2"];
            _resource.Set(create);

            var update = NewSchema(name);
            update.Members = ["TestUser3"];
            update.Purge = true;
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.Members.Should().NotContain("TestUser1");
            result.Members.Should().NotContain("TestUser2");
            result.Members.Should().Contain("TestUser3");
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; DROP ROLE IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_PurgeMembersEmpty_AllMembersRemoved()
    {
        var name = $"{Prefix}MembersPurgeEmpty1";
        try
        {
            var create = NewSchema(name);
            create.Members = ["TestUser1"];
            _resource.Set(create);

            var update = NewSchema(name);
            update.Members = [];
            update.Purge = true;
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.Members.Should().BeNullOrEmpty();
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; DROP ROLE IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_AddMemberToFixedRole_MemberAdded()
    {
        try
        {
            ExecuteSqlSafe($"USE [{TestDb}]; ALTER ROLE [db_datareader] DROP MEMBER [TestUser1]");

            var schema = NewSchema("db_datareader");
            schema.Members = ["TestUser1"];
            _resource.Set(schema);

            var result = _resource.Get(NewSchema("db_datareader"));
            result.Members.Should().Contain("TestUser1");
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; ALTER ROLE [db_datareader] DROP MEMBER [TestUser1]");
        }
    }

    [Fact]
    public void Delete_ExistingRole_RoleGone()
    {
        var name = $"{Prefix}Delete1";
        _resource.Set(NewSchema(name));

        var del = NewSchema(name);
        del.Exist = false;
        _resource.Delete(del);

        var result = _resource.Get(NewSchema(name));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Delete_NonExistentRole_DoesNotThrow()
    {
        var schema = NewSchema("NonExistentRole_ToDelete_XYZ");
        schema.Exist = false;
        var act = () => _resource.Delete(schema);
        act.Should().NotThrow();
    }
}
