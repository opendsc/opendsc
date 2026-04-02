// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using ServerPermissionResource = OpenDsc.Resource.SqlServer.ServerPermission.Resource;
using ServerPermissionSchema = OpenDsc.Resource.SqlServer.ServerPermission.Schema;
using PermissionState = Microsoft.SqlServer.Management.Smo.PermissionState;

using Xunit;

namespace OpenDsc.Resource.SqlServer.Tests.ServerPermission;

[Trait("Category", "Integration")]
public sealed class ServerPermissionTests : SqlServerTestBase
{
    private const string TestLogin = "OpenDscTest_SrvPerm_Login1";
    private const string TestServerRole = "OpenDscTest_SrvPerm_Role1";
    private readonly ServerPermissionResource _resource = new(SourceGenerationContext.Default);

    public ServerPermissionTests(SqlServerFixture fixture) : base(fixture)
    {
        ExecuteSqlSafe($"IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '{TestLogin}') CREATE LOGIN [{TestLogin}] WITH PASSWORD = 'T3stP@ssw0rd!SrvPerm123', CHECK_POLICY = OFF");
        ExecuteSqlSafe($"IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = '{TestServerRole}') CREATE SERVER ROLE [{TestServerRole}]");
    }

    private ServerPermissionSchema NewSchema(string principal, string permission) => new()
    {
        ServerInstance = ServerInstance,
        ConnectUsername = ConnectUsername,
        ConnectPassword = ConnectPassword,
        Principal = principal,
        Permission = permission
    };

    [Fact]
    public void Get_NonExistentPermission_ReturnsExistFalse()
    {
        var result = _resource.Get(NewSchema(TestLogin, "ViewServerState"));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Set_GrantPermission_PermissionGranted()
    {
        try
        {
            var schema = NewSchema(TestLogin, "ViewServerState");
            schema.State = PermissionState.Grant;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestLogin, "ViewServerState"));
            result.State.Should().Be(PermissionState.Grant);
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            ExecuteSqlSafe($"REVOKE VIEW SERVER STATE FROM [{TestLogin}]");
        }
    }

    [Fact]
    public void Set_GrantWithGrant_PermissionGrantedWithGrant()
    {
        try
        {
            var schema = NewSchema(TestLogin, "ViewServerState");
            schema.State = PermissionState.GrantWithGrant;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestLogin, "ViewServerState"));
            result.State.Should().Be(PermissionState.GrantWithGrant);
        }
        finally
        {
            ExecuteSqlSafe($"REVOKE GRANT OPTION FOR VIEW SERVER STATE FROM [{TestLogin}] CASCADE; REVOKE VIEW SERVER STATE FROM [{TestLogin}]");
        }
    }

    [Fact]
    public void Set_GrantThenDeny_PermissionDenied()
    {
        try
        {
            var grant = NewSchema(TestLogin, "ViewServerState");
            grant.State = PermissionState.Grant;
            _resource.Set(grant);

            var deny = NewSchema(TestLogin, "ViewServerState");
            deny.State = PermissionState.Deny;
            _resource.Set(deny);

            var result = _resource.Get(NewSchema(TestLogin, "ViewServerState"));
            result.State.Should().Be(PermissionState.Deny);
        }
        finally
        {
            ExecuteSqlSafe($"REVOKE VIEW SERVER STATE FROM [{TestLogin}]");
        }
    }

    [Fact]
    public void Set_DenyPermission_PermissionDenied()
    {
        try
        {
            var schema = NewSchema(TestLogin, "ViewServerState");
            schema.State = PermissionState.Deny;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestLogin, "ViewServerState"));
            result.State.Should().Be(PermissionState.Deny);
        }
        finally
        {
            ExecuteSqlSafe($"REVOKE VIEW SERVER STATE FROM [{TestLogin}]");
        }
    }

    [Fact]
    public void Set_GrantWithGrantThenGrant_PermissionDowngraded()
    {
        try
        {
            var grantWithGrant = NewSchema(TestLogin, "ViewServerState");
            grantWithGrant.State = PermissionState.GrantWithGrant;
            _resource.Set(grantWithGrant);

            var grant = NewSchema(TestLogin, "ViewServerState");
            grant.State = PermissionState.Grant;
            _resource.Set(grant);

            var result = _resource.Get(NewSchema(TestLogin, "ViewServerState"));
            result.State.Should().Be(PermissionState.Grant);
        }
        finally
        {
            ExecuteSqlSafe($"REVOKE VIEW SERVER STATE FROM [{TestLogin}]");
        }
    }

    [Fact]
    public void Delete_ExistingPermission_PermissionRevoked()
    {
        var grant = NewSchema(TestLogin, "ViewServerState");
        grant.State = PermissionState.Grant;
        _resource.Set(grant);

        var del = NewSchema(TestLogin, "ViewServerState");
        del.Exist = false;
        _resource.Delete(del);

        var result = _resource.Get(NewSchema(TestLogin, "ViewServerState"));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Delete_NonExistentPermission_DoesNotThrow()
    {
        var schema = NewSchema(TestLogin, "ViewServerState");
        schema.Exist = false;
        var act = () => _resource.Delete(schema);
        act.Should().NotThrow();
    }

    [Fact]
    public void Delete_DeniedPermission_PermissionRevoked()
    {
        var deny = NewSchema(TestLogin, "ViewServerState");
        deny.State = PermissionState.Deny;
        _resource.Set(deny);

        var del = NewSchema(TestLogin, "ViewServerState");
        del.Exist = false;
        _resource.Delete(del);

        var result = _resource.Get(NewSchema(TestLogin, "ViewServerState"));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Set_ServerRolePermission_PermissionGranted()
    {
        try
        {
            var schema = NewSchema(TestServerRole, "ViewServerState");
            schema.State = PermissionState.Grant;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestServerRole, "ViewServerState"));
            result.State.Should().Be(PermissionState.Grant);
        }
        finally
        {
            ExecuteSqlSafe($"REVOKE VIEW SERVER STATE FROM [{TestServerRole}]");
        }
    }

}
