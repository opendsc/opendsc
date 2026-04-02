// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using AgentJobResource = OpenDsc.Resource.SqlServer.AgentJob.Resource;
using AgentJobSchema = OpenDsc.Resource.SqlServer.AgentJob.Schema;
using CompletionAction = Microsoft.SqlServer.Management.Smo.Agent.CompletionAction;

using Xunit;

namespace OpenDsc.Resource.SqlServer.Tests.AgentJob;

[Trait("Category", "Integration")]
public sealed class AgentJobTests : SqlServerTestBase
{
    private const string Prefix = "OpenDscTest_AgentJob_";
    private readonly AgentJobResource _resource = new(SourceGenerationContext.Default);

    public AgentJobTests(SqlServerFixture fixture) : base(fixture) { }

    private AgentJobSchema NewSchema(string name) => new()
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
        schema.Should().Contain("isEnabled");
        schema.Should().Contain("eventLogLevel");
        schema.Should().Contain("https://json-schema.org/draft/2020-12/schema");
    }

    [Fact]
    public void Get_NonExistentJob_ReturnsExistFalse()
    {
        var result = _resource.Get(NewSchema("NonExistentJob_12345_XYZ"));
        result.Exist.Should().BeFalse();
        result.Name.Should().Be("NonExistentJob_12345_XYZ");
    }

    [Fact]
    public void Set_CreateJob_JobExists()
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
            ExecuteSqlSafe($"IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = '{name}') EXEC msdb.dbo.sp_delete_job @job_name = N'{name}'");
        }
    }

    [Fact]
    public void Set_CreateJobWithDescription_DescriptionSet()
    {
        var name = $"{Prefix}Desc1";
        try
        {
            var schema = NewSchema(name);
            schema.Description = "Test job description";
            schema.IsEnabled = true;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.Description.Should().Be("Test job description");
            result.IsEnabled.Should().BeTrue();
        }
        finally
        {
            ExecuteSqlSafe($"IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = '{name}') EXEC msdb.dbo.sp_delete_job @job_name = N'{name}'");
        }
    }

    [Fact]
    public void Set_UpdateJob_PropertiesUpdated()
    {
        var name = $"{Prefix}Update1";
        try
        {
            _resource.Set(NewSchema(name));

            var update = NewSchema(name);
            update.Description = "Updated description";
            update.IsEnabled = false;
            _resource.Set(update);

            var result = _resource.Get(NewSchema(name));
            result.Description.Should().Be("Updated description");
            result.IsEnabled.Should().BeFalse();
        }
        finally
        {
            ExecuteSqlSafe($"IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = '{name}') EXEC msdb.dbo.sp_delete_job @job_name = N'{name}'");
        }
    }

    [Fact]
    public void Set_SetEventLogLevel_LevelSet()
    {
        var name = $"{Prefix}EventLog1";
        try
        {
            var schema = NewSchema(name);
            schema.EventLogLevel = CompletionAction.OnFailure;
            _resource.Set(schema);

            var result = _resource.Get(NewSchema(name));
            result.EventLogLevel.Should().Be(CompletionAction.OnFailure);
        }
        finally
        {
            ExecuteSqlSafe($"IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = '{name}') EXEC msdb.dbo.sp_delete_job @job_name = N'{name}'");
        }
    }

    [Fact]
    public void Delete_ExistingJob_JobGone()
    {
        var name = $"{Prefix}Delete1";
        _resource.Set(NewSchema(name));

        _resource.Delete(NewSchema(name));

        var result = _resource.Get(NewSchema(name));
        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Delete_NonExistentJob_DoesNotThrow()
    {
        var act = () => _resource.Delete(NewSchema("NonExistentJob_ToDelete_XYZ"));
        act.Should().NotThrow();
    }

    [Fact]
    public void Export_ReturnsAgentJobs()
    {
        var name = $"{Prefix}Export1";
        try
        {
            _resource.Set(NewSchema(name));

            var filter = new AgentJobSchema
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
            ExecuteSqlSafe($"IF EXISTS (SELECT 1 FROM msdb.dbo.sysjobs WHERE name = '{name}') EXEC msdb.dbo.sp_delete_job @job_name = N'{name}'");
        }
    }
}
