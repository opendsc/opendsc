// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using DatabasePermissionResource = OpenDsc.Resource.SqlServer.DatabasePermission.Resource;
using DatabasePermissionSchema = OpenDsc.Resource.SqlServer.DatabasePermission.Schema;
using PermissionState = Microsoft.SqlServer.Management.Smo.PermissionState;

using Xunit;

namespace OpenDsc.Resource.SqlServer.Tests.DatabasePermission;

[Trait("Category", "Integration")]
public sealed class DatabasePermissionTests : SqlServerTestBase
{
    private const string TestDb = "OpenDscTest_DbPerm_DB";
    private const string TestUser = "OpenDscTest_DbPerm_User1";
    private const string TestRole = "OpenDscTest_DbPerm_Role1";
    private readonly DatabasePermissionResource _resource = new(SourceGenerationContext.Default);

    public DatabasePermissionTests(SqlServerFixture fixture) : base(fixture)
    {
        ExecuteSqlSafe($"IF DB_ID('{TestDb}') IS NULL CREATE DATABASE [{TestDb}]");
        ExecuteSqlSafe($"USE [{TestDb}]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '{TestUser}') CREATE USER [{TestUser}] WITHOUT LOGIN");
        ExecuteSqlSafe($"USE [{TestDb}]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '{TestRole}') CREATE ROLE [{TestRole}]");
    }

    private DatabasePermissionSchema NewSchema(string principal, string permission, string? db = null) => new()
    {
        ServerInstance = ServerInstance,
        ConnectUsername = ConnectUsername,
        ConnectPassword = ConnectPassword,
        DatabaseName = db ?? TestDb,
        Principal = principal,
        Permission = permission
    };

    [Fact]
    public void Get_NonExistentPermission_ReturnsExistFalse()
    {
        var result = _resource.Get(NewSchema(TestUser, "Select"));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Set_GrantPermission_PermissionGranted()
    {
        try
        {
            var schema = NewSchema(TestUser, "Connect");
            schema.State = PermissionState.Grant;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestUser, "Connect"));
            result.State.Should().Be(PermissionState.Grant);
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE CONNECT FROM [{TestUser}]");
        }
    }

    [Fact]
    public void Set_GrantWithGrant_PermissionGrantedWithGrant()
    {
        try
        {
            var schema = NewSchema(TestUser, "Connect");
            schema.State = PermissionState.GrantWithGrant;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestUser, "Connect"));
            result.State.Should().Be(PermissionState.GrantWithGrant);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE GRANT OPTION FOR CONNECT FROM [{TestUser}] CASCADE; REVOKE CONNECT FROM [{TestUser}]");
        }
    }

    [Fact]
    public void Set_GrantThenDeny_PermissionDenied()
    {
        try
        {
            var grant = NewSchema(TestUser, "Connect");
            grant.State = PermissionState.Grant;
            _resource.Set(grant);

            var deny = NewSchema(TestUser, "Connect");
            deny.State = PermissionState.Deny;
            _resource.Set(deny);

            var result = _resource.Get(NewSchema(TestUser, "Connect"));
            result.State.Should().Be(PermissionState.Deny);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE CONNECT FROM [{TestUser}]");
        }
    }

    [Fact]
    public void Set_DenyPermission_PermissionDenied()
    {
        try
        {
            var schema = NewSchema(TestUser, "Connect");
            schema.State = PermissionState.Deny;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestUser, "Connect"));
            result.State.Should().Be(PermissionState.Deny);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE CONNECT FROM [{TestUser}]");
        }
    }

    [Fact]
    public void Set_GrantWithGrantThenGrant_PermissionDowngraded()
    {
        try
        {
            var grantWithGrant = NewSchema(TestUser, "Connect");
            grantWithGrant.State = PermissionState.GrantWithGrant;
            _resource.Set(grantWithGrant);

            var grant = NewSchema(TestUser, "Connect");
            grant.State = PermissionState.Grant;
            _resource.Set(grant);

            var result = _resource.Get(NewSchema(TestUser, "Connect"));
            result.State.Should().Be(PermissionState.Grant);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE CONNECT FROM [{TestUser}]");
        }
    }

    [Fact]
    public void Delete_ExistingPermission_PermissionRevoked()
    {
        var grant = NewSchema(TestUser, "Connect");
        grant.State = PermissionState.Grant;
        _resource.Set(grant);

        var del = NewSchema(TestUser, "Connect");
        del.Exist = false;
        _resource.Delete(del);

        var result = _resource.Get(NewSchema(TestUser, "Connect"));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Delete_NonExistentPermission_DoesNotThrow()
    {
        var schema = NewSchema(TestUser, "Select");
        schema.Exist = false;
        var act = () => _resource.Delete(schema);
        act.Should().NotThrow();
    }

    [Fact]
    public void Delete_DeniedPermission_PermissionRevoked()
    {
        var deny = NewSchema(TestUser, "Connect");
        deny.State = PermissionState.Deny;
        _resource.Set(deny);

        var del = NewSchema(TestUser, "Connect");
        del.Exist = false;
        _resource.Delete(del);

        var result = _resource.Get(NewSchema(TestUser, "Connect"));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Set_RolePermission_PermissionGranted()
    {
        try
        {
            var schema = NewSchema(TestRole, "Connect");
            schema.State = PermissionState.Grant;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestRole, "Connect"));
            result.State.Should().Be(PermissionState.Grant);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE CONNECT FROM [{TestRole}]");
        }
    }

    [Fact]
    public void Export_ReturnsPermissionsInDatabase()
    {
        try
        {
            var schema = NewSchema(TestUser, "Connect");
            schema.State = PermissionState.Grant;
            _resource.Set(schema);

            var filter = new DatabasePermissionSchema
            {
                ServerInstance = ServerInstance,
                ConnectUsername = ConnectUsername,
                ConnectPassword = ConnectPassword,
                DatabaseName = TestDb,
                Principal = string.Empty,
                Permission = string.Empty
            };

            var results = _resource.Export(filter).ToList();
            results.Should().NotBeEmpty();
            results.Select(r => r.DatabaseName).Should().AllBe(TestDb);
            results.Should().Contain(r => r.Principal == TestUser && r.Permission == "Connect");
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE CONNECT FROM [{TestUser}]");
        }
    }

}
