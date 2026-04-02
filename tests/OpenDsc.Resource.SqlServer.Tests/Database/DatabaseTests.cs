// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using DatabaseResource = OpenDsc.Resource.SqlServer.Database.Resource;
using DatabaseSchema = OpenDsc.Resource.SqlServer.Database.Schema;
using Microsoft.SqlServer.Management.Smo;

using Xunit;

namespace OpenDsc.Resource.SqlServer.Tests.Database;

[Trait("Category", "Integration")]
public sealed class DatabaseTests : SqlServerTestBase
{
    private const string Prefix = "OpenDscTest_Db_";
    private readonly DatabaseResource _resource = new(SourceGenerationContext.Default);

    public DatabaseTests(SqlServerFixture fixture) : base(fixture) { }

    private DatabaseSchema NewSchema(string name) => new()
    {
        ServerInstance = ServerInstance,
        ConnectUsername = ConnectUsername,
        ConnectPassword = ConnectPassword,
        Name = name
    };

    private void DropDatabase(string name)
    {
        ExecuteSqlSafe($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]");
    }

    [Fact]
    public void GetSchema_ReturnsValidJsonSchema()
    {
        var schema = _resource.GetSchema();
        schema.Should().NotBeNullOrEmpty();
        schema.Should().Contain("serverInstance");
        schema.Should().Contain("name");
        schema.Should().Contain("collation");
        schema.Should().Contain("recoveryModel");
        schema.Should().Contain("https://json-schema.org/draft/2020-12/schema");
    }

    [Fact]
    public void Get_NonExistentDatabase_ReturnsExistFalse()
    {
        var result = _resource.Get(NewSchema("NonExistentDatabase_12345_XYZ"));
        result.Exist.Should().BeFalse();
        result.Name.Should().Be("NonExistentDatabase_12345_XYZ");
    }

    [Fact]
    public void Get_MasterDatabase_ReturnsProperties()
    {
        var result = _resource.Get(NewSchema("master"));
        result.Name.Should().Be("master");
        result.IsSystemObject.Should().BeTrue();
        result.Exist.Should().NotBe(false);
        result.Collation.Should().NotBeNullOrEmpty();
        result.RecoveryModel.Should().NotBeNull();
    }

    [Fact]
    public void Get_TempDb_ReturnsMetadata()
    {
        var result = _resource.Get(NewSchema("tempdb"));
        result.Id.Should().BeGreaterThan(0);
        result.CreateDate.Should().NotBeNull();
        result.Size.Should().BeGreaterThan(0);
        result.IsAccessible.Should().BeTrue();
    }

    [Fact]
    public void Set_CreateDatabaseWithDefaults_DatabaseExists()
    {
        var name = $"{Prefix}Create1";
        try
        {
            _resource.Set(NewSchema(name));

            var result = _resource.Get(NewSchema(name));
            result.Name.Should().Be(name);
            result.Exist.Should().NotBe(false);
            result.IsSystemObject.Should().BeFalse();
        }
        finally
        {
            DropDatabase(name);
        }
    }

    [Fact]
    public void Set_CreateDatabaseWithRecoveryModel_RecoveryModelSet()
    {
        var name = $"{Prefix}Create2";
        try
        {
            var schema = NewSchema(name);
            schema.RecoveryModel = RecoveryModel.Simple;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.RecoveryModel.Should().Be(RecoveryModel.Simple);
        }
        finally
        {
            DropDatabase(name);
        }
    }

    [Fact]
    public void Set_UpdateDatabaseProperties_PropertiesUpdated()
    {
        var name = $"{Prefix}Update1";
        try
        {
            var create = NewSchema(name);
            create.RecoveryModel = RecoveryModel.Full;
            _resource.Set(create);

            var update = NewSchema(name);
            update.RecoveryModel = RecoveryModel.Simple;
            update.AutoShrink = true;
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.RecoveryModel.Should().Be(RecoveryModel.Simple);
            result.AutoShrink.Should().BeTrue();
        }
        finally
        {
            DropDatabase(name);
        }
    }

    [Fact]
    public void Set_AnsiOptions_OptionsSet()
    {
        var name = $"{Prefix}Ansi1";
        try
        {
            var schema = NewSchema(name);
            schema.AnsiNullDefault = true;
            schema.AnsiNullsEnabled = true;
            schema.AnsiPaddingEnabled = true;
            schema.QuotedIdentifiersEnabled = true;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.AnsiNullDefault.Should().BeTrue();
            result.AnsiNullsEnabled.Should().BeTrue();
            result.AnsiPaddingEnabled.Should().BeTrue();
            result.QuotedIdentifiersEnabled.Should().BeTrue();
        }
        finally
        {
            DropDatabase(name);
        }
    }

    [Fact]
    public void Delete_ExistingDatabase_DatabaseGone()
    {
        var name = $"{Prefix}Delete1";
        _resource.Set(NewSchema(name));

        _resource.Delete(NewSchema(name));

        var result = _resource.Get(NewSchema(name));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Delete_NonExistentDatabase_DoesNotThrow()
    {
        var act = () => _resource.Delete(NewSchema("NonExistentDatabase_ToDelete_XYZ"));
        act.Should().NotThrow();
    }

    [Fact]
    public void Export_ReturnsUserDatabases()
    {
        var name = $"{Prefix}Export1";
        try
        {
            _resource.Set(NewSchema(name));

            var filter = new DatabaseSchema
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
            DropDatabase(name);
        }
    }
}
