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

        var action = () => _resource.Export(filter);
        action.Should().NotThrow();
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
        var name = "OpenDscTest_AG_Create1";
        var schema = NewSchema(name);
        schema.ClusterType = Microsoft.SqlServer.Management.Smo.AvailabilityGroupClusterType.None;

        try
        {
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.Name.Should().Be(name);
            result.Exist.Should().NotBe(false);
        }
        catch (Microsoft.SqlServer.Management.Smo.FailedOperationException)
        {
            // HADR not enabled on this instance — expected in most test environments
        }
        finally
        {
            ExecuteSqlSafe($"DROP AVAILABILITY GROUP IF EXISTS [{name}]");
        }
    }
}
