// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using ServerRoleResource = OpenDsc.Resource.SqlServer.ServerRole.Resource;
using ServerRoleSchema = OpenDsc.Resource.SqlServer.ServerRole.Schema;

using Xunit;

namespace OpenDsc.Resource.SqlServer.Tests.ServerRole;

[Trait("Category", "Integration")]
public sealed class ServerRoleTests : SqlServerTestBase
{
    private const string Prefix = "OpenDscTest_ServerRole_";
    private const string TestLogin1 = "OpenDscTest_ServerRole_Login1";
    private const string TestLogin2 = "OpenDscTest_ServerRole_Login2";
    private const string TestLogin3 = "OpenDscTest_ServerRole_Login3";
    private readonly ServerRoleResource _resource = new(SourceGenerationContext.Default);

    public ServerRoleTests(SqlServerFixture fixture) : base(fixture)
    {
        ExecuteSqlSafe($"IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '{TestLogin1}') CREATE LOGIN [{TestLogin1}] WITH PASSWORD = 'T3stP@ssw0rd!Role123', CHECK_POLICY = OFF");
        ExecuteSqlSafe($"IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '{TestLogin2}') CREATE LOGIN [{TestLogin2}] WITH PASSWORD = 'T3stP@ssw0rd!Role456', CHECK_POLICY = OFF");
        ExecuteSqlSafe($"IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '{TestLogin3}') CREATE LOGIN [{TestLogin3}] WITH PASSWORD = 'T3stP@ssw0rd!Role789', CHECK_POLICY = OFF");
    }

    private ServerRoleSchema NewSchema(string name) => new()
    {
        ServerInstance = ServerInstance,
        ConnectUsername = ConnectUsername,
        ConnectPassword = ConnectPassword,
        Name = name
    };

    [Fact]
    public void Get_NonExistentRole_ReturnsExistFalse()
    {
        var result = _resource.Get(NewSchema("NonExistentServerRole_12345_XYZ"));
        result.Exist.Should().BeFalse();
        result.Name.Should().Be("NonExistentServerRole_12345_XYZ");
    }

    [Fact]
    public void Get_SysadminRole_ReturnsIsFixedRole()
    {
        var result = _resource.Get(NewSchema("sysadmin"));
        result.Name.Should().Be("sysadmin");
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
            ExecuteSqlSafe($"DROP SERVER ROLE IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_CreateRoleWithMembers_MembersAssigned()
    {
        var name = $"{Prefix}Members1";
        try
        {
            var schema = NewSchema(name);
            schema.Members = [TestLogin1, TestLogin2];
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.Members.Should().Contain(TestLogin1);
            result.Members.Should().Contain(TestLogin2);
        }
        finally
        {
            ExecuteSqlSafe($"DROP SERVER ROLE IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_ChangeOwner_OwnerUpdated()
    {
        var name = $"{Prefix}Owner1";
        try
        {
            _resource.Set(NewSchema(name));

            var update = NewSchema(name);
            update.Owner = "sa";
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.Owner.Should().Be("sa");
        }
        finally
        {
            ExecuteSqlSafe($"ALTER SERVER ROLE [{name}] WITH NAME = [{name}]"); // owner reset won't matter after drop
            ExecuteSqlSafe($"DROP SERVER ROLE IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_AddMembersAdditive_AllMembersPresent()
    {
        var name = $"{Prefix}MembersAdd1";
        try
        {
            var create = NewSchema(name);
            create.Members = [TestLogin1];
            _resource.Set(create);

            var update = NewSchema(name);
            update.Members = [TestLogin2];
            update.Purge = false;
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.Members.Should().Contain(TestLogin1);
            result.Members.Should().Contain(TestLogin2);
        }
        finally
        {
            ExecuteSqlSafe($"DROP SERVER ROLE IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_PurgeMembers_OnlyNewMembersPresent()
    {
        var name = $"{Prefix}MembersPurge1";
        try
        {
            var create = NewSchema(name);
            create.Members = [TestLogin1, TestLogin2];
            _resource.Set(create);

            var update = NewSchema(name);
            update.Members = [TestLogin3];
            update.Purge = true;
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.Members.Should().NotContain(TestLogin1);
            result.Members.Should().NotContain(TestLogin2);
            result.Members.Should().Contain(TestLogin3);
        }
        finally
        {
            ExecuteSqlSafe($"DROP SERVER ROLE IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_PurgeMembersEmpty_AllMembersRemoved()
    {
        var name = $"{Prefix}MembersPurgeEmpty1";
        try
        {
            var create = NewSchema(name);
            create.Members = [TestLogin1];
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
            ExecuteSqlSafe($"DROP SERVER ROLE IF EXISTS [{name}]");
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
        var schema = NewSchema("NonExistentServerRole_ToDelete_XYZ");
        schema.Exist = false;
        var act = () => _resource.Delete(schema);
        act.Should().NotThrow();
    }

    [Fact]
    public void Export_ReturnsServerRoles()
    {
        var name = $"{Prefix}Export1";
        try
        {
            _resource.Set(NewSchema(name));

            var filter = new ServerRoleSchema
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
            ExecuteSqlSafe($"DROP SERVER ROLE IF EXISTS [{name}]");
        }
    }
}
