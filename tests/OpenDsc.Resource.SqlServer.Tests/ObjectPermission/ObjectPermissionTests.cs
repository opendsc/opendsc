// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using ObjectPermissionResource = OpenDsc.Resource.SqlServer.ObjectPermission.Resource;
using ObjectPermissionSchema = OpenDsc.Resource.SqlServer.ObjectPermission.Schema;
using ObjectType = OpenDsc.Resource.SqlServer.ObjectPermission.ObjectType;
using PermissionState = Microsoft.SqlServer.Management.Smo.PermissionState;

using Xunit;

namespace OpenDsc.Resource.SqlServer.Tests.ObjectPermission;

[Trait("Category", "Integration")]
public sealed class ObjectPermissionTests : SqlServerTestBase
{
    private const string TestDb = "OpenDscTest_ObjPerm_DB";
    private const string TestUser1 = "OpenDscTest_ObjPerm_User1";
    private const string TestUser2 = "OpenDscTest_ObjPerm_User2";
    private const string TestTable = "OpenDscTest_ObjPerm_Table";
    private const string TestView = "OpenDscTest_ObjPerm_View";
    private const string TestProc = "OpenDscTest_ObjPerm_Proc";
    private const string TestFunc = "OpenDscTest_ObjPerm_Func";
    private const string TestSchema = "OpenDscTest_ObjPerm_Schema";
    private readonly ObjectPermissionResource _resource = new(SourceGenerationContext.Default);

    public ObjectPermissionTests(SqlServerFixture fixture) : base(fixture)
    {
        ExecuteSqlSafe($"IF DB_ID('{TestDb}') IS NULL CREATE DATABASE [{TestDb}]");
        ExecuteSqlSafe($"USE [{TestDb}]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '{TestUser1}') CREATE USER [{TestUser1}] WITHOUT LOGIN");
        ExecuteSqlSafe($"USE [{TestDb}]; IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = '{TestUser2}') CREATE USER [{TestUser2}] WITHOUT LOGIN");
        ExecuteSqlSafe($"USE [{TestDb}]; IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = '{TestSchema}') EXEC('CREATE SCHEMA [{TestSchema}]')");
        ExecuteSqlSafe($"USE [{TestDb}]; IF OBJECT_ID('{TestTable}') IS NULL CREATE TABLE [{TestTable}] (Id INT PRIMARY KEY)");
        ExecuteSqlSafe($"USE [{TestDb}]; IF OBJECT_ID('{TestView}', 'V') IS NULL EXEC('CREATE VIEW [{TestView}] AS SELECT 1 AS Id')");
        ExecuteSqlSafe($"USE [{TestDb}]; IF OBJECT_ID('{TestProc}', 'P') IS NULL EXEC('CREATE PROCEDURE [{TestProc}] AS SELECT 1')");
        ExecuteSqlSafe($"USE [{TestDb}]; IF OBJECT_ID('{TestFunc}', 'FN') IS NULL EXEC('CREATE FUNCTION [{TestFunc}]() RETURNS INT AS BEGIN RETURN 1 END')");
    }

    private ObjectPermissionSchema NewSchema(string principal, string permission, ObjectType objectType, string objectName, string? schemaName = "dbo") => new()
    {
        ServerInstance = ServerInstance,
        ConnectUsername = ConnectUsername,
        ConnectPassword = ConnectPassword,
        DatabaseName = TestDb,
        Principal = principal,
        Permission = permission,
        ObjectType = objectType,
        ObjectName = objectName,
        SchemaName = schemaName
    };

    [Fact]
    public void Get_NonExistentPermission_ReturnsExistFalse()
    {
        var result = _resource.Get(NewSchema(TestUser1, "Select", ObjectType.Table, TestTable));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Get_DefaultsSchemaNameToDbo()
    {
        var schema = new ObjectPermissionSchema
        {
            ServerInstance = ServerInstance,
            ConnectUsername = ConnectUsername,
            ConnectPassword = ConnectPassword,
            DatabaseName = TestDb,
            Principal = TestUser1,
            Permission = "Select",
            ObjectType = ObjectType.Table,
            ObjectName = TestTable
        };

        var result = _resource.Get(schema);
        result.SchemaName.Should().Be("dbo");
    }

    [Fact]
    public void Set_TableSelectPermission_PermissionGranted()
    {
        try
        {
            var schema = NewSchema(TestUser1, "Select", ObjectType.Table, TestTable);
            schema.State = PermissionState.Grant;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestUser1, "Select", ObjectType.Table, TestTable));
            result.State.Should().Be(PermissionState.Grant);
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE SELECT ON [{TestTable}] FROM [{TestUser1}]");
        }
    }

    [Fact]
    public void Set_TableInsertPermission_PermissionGranted()
    {
        try
        {
            var schema = NewSchema(TestUser1, "Insert", ObjectType.Table, TestTable);
            schema.State = PermissionState.Grant;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestUser1, "Insert", ObjectType.Table, TestTable));
            result.State.Should().Be(PermissionState.Grant);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE INSERT ON [{TestTable}] FROM [{TestUser1}]");
        }
    }

    [Fact]
    public void Set_TableDenyPermission_PermissionDenied()
    {
        try
        {
            var schema = NewSchema(TestUser1, "Select", ObjectType.Table, TestTable);
            schema.State = PermissionState.Deny;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestUser1, "Select", ObjectType.Table, TestTable));
            result.State.Should().Be(PermissionState.Deny);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE SELECT ON [{TestTable}] FROM [{TestUser1}]");
        }
    }

    [Fact]
    public void Set_TableGrantWithGrant_PermissionGrantedWithGrant()
    {
        try
        {
            var schema = NewSchema(TestUser1, "Select", ObjectType.Table, TestTable);
            schema.State = PermissionState.GrantWithGrant;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestUser1, "Select", ObjectType.Table, TestTable));
            result.State.Should().Be(PermissionState.GrantWithGrant);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE GRANT OPTION FOR SELECT ON [{TestTable}] FROM [{TestUser1}] CASCADE; REVOKE SELECT ON [{TestTable}] FROM [{TestUser1}]");
        }
    }

    [Fact]
    public void Set_ViewSelectPermission_PermissionGranted()
    {
        try
        {
            var schema = NewSchema(TestUser1, "Select", ObjectType.View, TestView);
            schema.State = PermissionState.Grant;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestUser1, "Select", ObjectType.View, TestView));
            result.State.Should().Be(PermissionState.Grant);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE SELECT ON [{TestView}] FROM [{TestUser1}]");
        }
    }

    [Fact]
    public void Set_StoredProcedureExecutePermission_PermissionGranted()
    {
        try
        {
            var schema = NewSchema(TestUser1, "Execute", ObjectType.StoredProcedure, TestProc);
            schema.State = PermissionState.Grant;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestUser1, "Execute", ObjectType.StoredProcedure, TestProc));
            result.State.Should().Be(PermissionState.Grant);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE EXECUTE ON [{TestProc}] FROM [{TestUser1}]");
        }
    }

    [Fact]
    public void Set_FunctionExecutePermission_PermissionGranted()
    {
        try
        {
            var schema = NewSchema(TestUser1, "Execute", ObjectType.UserDefinedFunction, TestFunc);
            schema.State = PermissionState.Grant;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestUser1, "Execute", ObjectType.UserDefinedFunction, TestFunc));
            result.State.Should().Be(PermissionState.Grant);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE EXECUTE ON [{TestFunc}] FROM [{TestUser1}]");
        }
    }

    [Fact]
    public void Set_SchemaPermission_PermissionGranted()
    {
        try
        {
            var schema = NewSchema(TestUser1, "Select", ObjectType.Schema, TestSchema, null);
            schema.State = PermissionState.Grant;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(TestUser1, "Select", ObjectType.Schema, TestSchema, null));
            result.State.Should().Be(PermissionState.Grant);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE SELECT ON SCHEMA::[{TestSchema}] FROM [{TestUser1}]");
        }
    }

    [Fact]
    public void Delete_ExistingPermission_PermissionRevoked()
    {
        var grant = NewSchema(TestUser2, "Select", ObjectType.Table, TestTable);
        grant.State = PermissionState.Grant;
        _resource.Set(grant);

        var del = NewSchema(TestUser2, "Select", ObjectType.Table, TestTable);
        del.Exist = false;
        _resource.Delete(del);

        var result = _resource.Get(NewSchema(TestUser2, "Select", ObjectType.Table, TestTable));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Delete_NonExistentPermission_DoesNotThrow()
    {
        var schema = NewSchema(TestUser2, "Insert", ObjectType.Table, TestTable);
        schema.Exist = false;
        var act = () => _resource.Delete(schema);
        act.Should().NotThrow();
    }

    [Fact]
    public void Delete_DeniedPermission_PermissionRevoked()
    {
        var deny = NewSchema(TestUser2, "Update", ObjectType.Table, TestTable);
        deny.State = PermissionState.Deny;
        _resource.Set(deny);

        var del = NewSchema(TestUser2, "Update", ObjectType.Table, TestTable);
        del.Exist = false;
        _resource.Delete(del);

        var result = _resource.Get(NewSchema(TestUser2, "Update", ObjectType.Table, TestTable));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Export_ReturnsObjectPermissions()
    {
        try
        {
            var schema = NewSchema(TestUser1, "Select", ObjectType.Table, TestTable);
            schema.State = PermissionState.Grant;
            _resource.Set(schema);

            var filter = new ObjectPermissionSchema
            {
                ServerInstance = ServerInstance,
                ConnectUsername = ConnectUsername,
                ConnectPassword = ConnectPassword,
                DatabaseName = TestDb,
                Principal = string.Empty,
                Permission = string.Empty,
                ObjectName = string.Empty
            };

            var results = _resource.Export(filter).ToList();
            results.Should().NotBeEmpty();
            results.Select(r => r.DatabaseName).Should().AllBe(TestDb);
            results.Should().Contain(r => r.Principal == TestUser1 && r.Permission == "Select" && r.ObjectName == TestTable);
        }
        finally
        {
            ExecuteSqlSafe($"USE [{TestDb}]; REVOKE SELECT ON [{TestTable}] FROM [{TestUser1}]");
        }
    }

}
