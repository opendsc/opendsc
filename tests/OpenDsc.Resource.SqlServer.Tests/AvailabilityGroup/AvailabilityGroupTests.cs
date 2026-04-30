// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using AvailabilityGroupResource = OpenDsc.Resource.SqlServer.AvailabilityGroup.Resource;
using AvailabilityGroupSchema = OpenDsc.Resource.SqlServer.AvailabilityGroup.Schema;

using Xunit;

namespace OpenDsc.Resource.SqlServer.Tests.AvailabilityGroup;

[Trait("Category", "Integration")]
public sealed class AvailabilityGroupTests : SqlServerTestBase
{
    private const string Prefix = "OpenDscTest_AG_";
    private readonly AvailabilityGroupResource _resource = new(SourceGenerationContext.Default);

    public AvailabilityGroupTests(SqlServerFixture fixture) : base(fixture)
    {
    }

    private AvailabilityGroupSchema NewSchema(string name) => new()
    {
        ServerInstance = ServerInstance,
        ConnectUsername = ConnectUsername,
        ConnectPassword = ConnectPassword,
        Name = name
    };

    private bool IsHadrEnabled()
    {
        var server = ConnectToServer();
        try
        {
            server.ConnectionContext.Connect();
            var result = server.ConnectionContext.ExecuteScalar("SELECT CAST(SERVERPROPERTY('IsHadrEnabled') AS int)");
            return result is int i && i == 1;
        }
        catch
        {
            return false;
        }
        finally
        {
            OpenDsc.Resource.SqlServer.SqlConnectionHelper.SafeDisconnect(server);
        }
    }

    [Fact]
    public void GetSchema_ReturnsValidJsonSchema()
    {
        var schema = _resource.GetSchema();
        schema.Should().NotBeNullOrEmpty();
        schema.Should().Contain("\"name\"");
        schema.Should().Contain("\"serverInstance\"");
        schema.Should().Contain("\"_exist\"");
    }

    [Fact]
    public void Get_NonExistentAvailabilityGroup_ReturnsExistFalse()
    {
        var result = _resource.Get(NewSchema("NonExistentAG_12345_XYZ"));
        result.Exist.Should().BeFalse();
        result.Name.Should().Be("NonExistentAG_12345_XYZ");
    }

    [Fact]
    public void Export_ReturnsAvailabilityGroups()
    {
        var filter = new AvailabilityGroupSchema
        {
            ServerInstance = ServerInstance,
            ConnectUsername = ConnectUsername,
            ConnectPassword = ConnectPassword,
            Name = string.Empty
        };

        var results = _resource.Export(filter).ToList();
        results.Should().NotBeNull();

        foreach (var ag in results)
        {
            ag.Name.Should().NotBeNullOrEmpty();
            ag.ServerInstance.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public void Delete_NonExistentAvailabilityGroup_DoesNotThrow()
    {
        var action = () => _resource.Delete(NewSchema("NonExistentAG_Delete_12345_XYZ"));
        action.Should().NotThrow();
    }

    [Fact]
    public void Set_CreateAvailabilityGroup_RequiresHadr()
    {
        Assert.SkipUnless(IsHadrEnabled(), "Always On HADR is not enabled on this instance");

        var name = $"{Prefix}Create_{Guid.NewGuid():N}";
        var schema = NewSchema(name);
        schema.ClusterType = Microsoft.SqlServer.Management.Smo.AvailabilityGroupClusterType.None;

        try
        {
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.Name.Should().Be(name);
            result.Exist.Should().NotBe(false);
        }
        finally
        {
            ExecuteSqlSafe($"DROP AVAILABILITY GROUP IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Set_CreateWithAllProperties_AppliesAllSettings()
    {
        Assert.SkipUnless(IsHadrEnabled(), "Always On HADR is not enabled on this instance");

        var name = $"{Prefix}AllProps_{Guid.NewGuid():N}";
        var schema = NewSchema(name);
        schema.AutomatedBackupPreference = Microsoft.SqlServer.Management.Smo.AvailabilityGroupAutomatedBackupPreference.Secondary;
        schema.FailureConditionLevel = Microsoft.SqlServer.Management.Smo.AvailabilityGroupFailureConditionLevel.OnServerDown;
        schema.HealthCheckTimeout = 30000;
        schema.BasicAvailabilityGroup = false;
        schema.DatabaseHealthTrigger = true;
        schema.DtcSupportEnabled = false;
        schema.ClusterType = Microsoft.SqlServer.Management.Smo.AvailabilityGroupClusterType.None;
        schema.RequiredSynchronizedSecondariesToCommit = 0;
        schema.IsContained = false;

        try
        {
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.Name.Should().Be(name);
            result.Exist.Should().NotBe(false);
            result.AutomatedBackupPreference.Should().Be(schema.AutomatedBackupPreference);
            result.FailureConditionLevel.Should().Be(schema.FailureConditionLevel);
            result.HealthCheckTimeout.Should().Be(schema.HealthCheckTimeout);
            result.DatabaseHealthTrigger.Should().Be(schema.DatabaseHealthTrigger);
            result.DtcSupportEnabled.Should().Be(schema.DtcSupportEnabled);
            result.ClusterType.Should().Be(schema.ClusterType);
            result.RequiredSynchronizedSecondariesToCommit.Should().Be(schema.RequiredSynchronizedSecondariesToCommit);
        }
        finally
        {
            ExecuteSqlSafe($"DROP AVAILABILITY GROUP IF EXISTS [{name}]");
        }
    }

    [Fact]
    public void Get_NullInstance_Throws()
    {
        var action = () => _resource.Get(null);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Set_NullInstance_Throws()
    {
        var action = () => _resource.Set(null);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Delete_NullInstance_Throws()
    {
        var action = () => _resource.Delete(null);
        action.Should().Throw<ArgumentNullException>();
    }
}
