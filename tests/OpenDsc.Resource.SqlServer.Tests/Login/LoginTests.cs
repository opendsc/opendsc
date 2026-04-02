// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using LoginType = Microsoft.SqlServer.Management.Smo.LoginType;

using Xunit;

using LoginResource = OpenDsc.Resource.SqlServer.Login.Resource;
using LoginSchema = OpenDsc.Resource.SqlServer.Login.Schema;

namespace OpenDsc.Resource.SqlServer.Tests.Login;

[Trait("Category", "Integration")]
public sealed class LoginTests : SqlServerTestBase
{
    private const string Prefix = "OpenDscTest_Login_";
    private readonly LoginResource _resource = new(SourceGenerationContext.Default);

    public LoginTests(SqlServerFixture fixture) : base(fixture) { }

    private LoginSchema NewSchema(string name) => new()
    {
        ServerInstance = ServerInstance,
        ConnectUsername = ConnectUsername,
        ConnectPassword = ConnectPassword,
        Name = name
    };

    [Fact]
    public void GetSchema_ReturnsValidJsonSchema()
    {
        var schema = _resource.GetSchema();
        schema.Should().NotBeNullOrEmpty();
        schema.Should().Contain("serverInstance");
        schema.Should().Contain("name");
        schema.Should().Contain("loginType");
        schema.Should().Contain("https://json-schema.org/draft/2020-12/schema");
    }

    [Fact]
    public void Get_NonExistentLogin_ReturnsExistFalse()
    {
        var result = _resource.Get(NewSchema("NonExistentLogin_12345_XYZ"));
        result.Exist.Should().BeFalse();
        result.Name.Should().Be("NonExistentLogin_12345_XYZ");
    }

    [Fact]
    public void Get_SaLogin_ReturnsLoginProperties()
    {
        var result = _resource.Get(NewSchema("sa"));
        result.Name.Should().Be("sa");
        result.LoginType.Should().Be(LoginType.SqlLogin);
        result.Exist.Should().NotBe(false);
    }

    [Fact]
    public void Set_CreateSqlLogin_LoginExists()
    {
        var name = $"{Prefix}Create1";
        try
        {
            var schema = NewSchema(name);
            schema.LoginType = LoginType.SqlLogin;
            schema.Password = "T3stP@ssw0rd!Secure123";
            schema.PasswordPolicyEnforced = true;
            schema.PasswordExpirationEnabled = false;

            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.Name.Should().Be(name);
            result.LoginType.Should().Be(LoginType.SqlLogin);
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            _resource.Delete(NewSchema(name));
        }
    }

    [Fact]
    public void Set_UpdateSqlLogin_PropertiesUpdated()
    {
        var name = $"{Prefix}Update1";
        try
        {
            var create = NewSchema(name);
            create.LoginType = LoginType.SqlLogin;
            create.Password = "T3stP@ssw0rd!Initial123";
            create.DefaultDatabase = "master";
            _resource.Set(create);

            var update = NewSchema(name);
            update.DefaultDatabase = "tempdb";
            update.Disabled = true;
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.DefaultDatabase.Should().Be("tempdb");
            result.Disabled.Should().BeTrue();
        }
        finally
        {
            _resource.Delete(NewSchema(name));
        }
    }

    [Fact]
    public void Set_CreateDisabledLogin_LoginIsDisabled()
    {
        var name = $"{Prefix}Disabled1";
        try
        {
            var schema = NewSchema(name);
            schema.LoginType = LoginType.SqlLogin;
            schema.Password = "T3stP@ssw0rd!Disabled123";
            schema.Disabled = true;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.Name.Should().Be(name);
            result.Disabled.Should().BeTrue();
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            _resource.Delete(NewSchema(name));
        }
    }

    [Fact]
    public void Set_ChangePassword_LoginStillExists()
    {
        var name = $"{Prefix}PwdChange1";
        try
        {
            var create = NewSchema(name);
            create.LoginType = LoginType.SqlLogin;
            create.Password = "T3stP@ssw0rd!Initial123";
            _resource.Set(create);

            var update = NewSchema(name);
            update.Password = "T3stP@ssw0rd!Changed456";
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.Name.Should().Be(name);
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            _resource.Delete(NewSchema(name));
        }
    }

    [Fact]
    public void Set_EnablePasswordPolicy_PolicyEnabled()
    {
        var name = $"{Prefix}Policy1";
        try
        {
            var create = NewSchema(name);
            create.LoginType = LoginType.SqlLogin;
            create.Password = "T3stP@ssw0rd!Policy123";
            create.PasswordPolicyEnforced = false;
            _resource.Set(create);

            var update = NewSchema(name);
            update.PasswordPolicyEnforced = true;
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.PasswordPolicyEnforced.Should().BeTrue();
        }
        finally
        {
            _resource.Delete(NewSchema(name));
        }
    }

    [Fact]
    public void Set_DisablePasswordPolicy_PolicyDisabled()
    {
        var name = $"{Prefix}Policy2";
        try
        {
            var create = NewSchema(name);
            create.LoginType = LoginType.SqlLogin;
            create.Password = "T3stP@ssw0rd!Policy456";
            create.PasswordPolicyEnforced = true;
            _resource.Set(create);

            var update = NewSchema(name);
            update.PasswordPolicyEnforced = false;
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.PasswordPolicyEnforced.Should().BeFalse();
        }
        finally
        {
            _resource.Delete(NewSchema(name));
        }
    }

    [Fact]
    public void Set_AssignServerRoles_RolesAssigned()
    {
        var name = $"{Prefix}Roles1";
        try
        {
            var schema = NewSchema(name);
            schema.LoginType = LoginType.SqlLogin;
            schema.Password = "T3stP@ssw0rd!Roles123";
            schema.ServerRoles = ["dbcreator", "securityadmin"];
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.ServerRoles.Should().Contain("dbcreator");
            result.ServerRoles.Should().Contain("securityadmin");
        }
        finally
        {
            _resource.Delete(NewSchema(name));
        }
    }

    [Fact]
    public void Set_AddServerRolesAdditive_AllRolesPresent()
    {
        var name = $"{Prefix}RolesAdd1";
        try
        {
            var create = NewSchema(name);
            create.LoginType = LoginType.SqlLogin;
            create.Password = "T3stP@ssw0rd!RolesAdd123";
            create.ServerRoles = ["dbcreator"];
            _resource.Set(create);

            var update = NewSchema(name);
            update.ServerRoles = ["securityadmin", "processadmin"];
            update.Purge = false;
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.ServerRoles.Should().Contain("dbcreator");
            result.ServerRoles.Should().Contain("securityadmin");
            result.ServerRoles.Should().Contain("processadmin");
        }
        finally
        {
            _resource.Delete(NewSchema(name));
        }
    }

    [Fact]
    public void Set_PurgeServerRoles_OnlyNewRolesPresent()
    {
        var name = $"{Prefix}RolesPurge1";
        try
        {
            var create = NewSchema(name);
            create.LoginType = LoginType.SqlLogin;
            create.Password = "T3stP@ssw0rd!RolesPurge123";
            create.ServerRoles = ["dbcreator", "bulkadmin"];
            _resource.Set(create);

            var update = NewSchema(name);
            update.ServerRoles = ["securityadmin", "processadmin"];
            update.Purge = true;
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.ServerRoles.Should().NotContain("dbcreator");
            result.ServerRoles.Should().NotContain("bulkadmin");
            result.ServerRoles.Should().Contain("securityadmin");
            result.ServerRoles.Should().Contain("processadmin");
        }
        finally
        {
            _resource.Delete(NewSchema(name));
        }
    }

    [Fact]
    public void Delete_ExistingLogin_LoginGone()
    {
        var name = $"{Prefix}Delete1";
        var create = NewSchema(name);
        create.LoginType = LoginType.SqlLogin;
        create.Password = "T3stP@ssw0rd!Delete123";
        _resource.Set(create);

        _resource.Delete(NewSchema(name));

        var result = _resource.Get(NewSchema(name));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Delete_NonExistentLogin_DoesNotThrow()
    {
        var act = () => _resource.Delete(NewSchema("NonExistentLogin_ToDelete_XYZ"));
        act.Should().NotThrow();
    }
}
