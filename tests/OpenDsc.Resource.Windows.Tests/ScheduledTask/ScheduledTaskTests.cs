// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using AwesomeAssertions;

using Xunit;

using Action = OpenDsc.Resource.Windows.ScheduledTask.Action;
using LogonTriggerConfig = OpenDsc.Resource.Windows.ScheduledTask.LogonTriggerConfig;
using ScheduledTaskResource = OpenDsc.Resource.Windows.ScheduledTask.Resource;
using ScheduledTaskSchema = OpenDsc.Resource.Windows.ScheduledTask.Schema;
using Trigger = OpenDsc.Resource.Windows.ScheduledTask.Trigger;

namespace OpenDsc.Resource.Windows.Tests.ScheduledTask;

[Trait("Category", "Integration")]
public sealed class ScheduledTaskTests : WindowsTestBase
{
    private readonly ScheduledTaskResource _resource = new(OpenDsc.Resource.Windows.SourceGenerationContext.Default);

    private static ScheduledTaskSchema BuildBaseSchema(string taskName, string description)
    {
        return new ScheduledTaskSchema
        {
            TaskName = taskName,
            Description = description,
            Triggers = new[]
            {
                new Trigger
                {
                    Logon = new LogonTriggerConfig
                    {
                        Enabled = true
                    }
                }
            },
            Actions = new[]
            {
                new Action
                {
                    Path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Windows), "System32", "cmd.exe"),
                    Arguments = "/c echo ScheduledTaskTests"
                }
            }
        };
    }

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = System.Text.Json.JsonDocument.Parse(schemaJson);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectTypeAndVersion()
    {
        var attr = typeof(ScheduledTaskResource).GetCustomAttributes(typeof(DscResourceAttribute), false)
            .OfType<DscResourceAttribute>().SingleOrDefault();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/ScheduledTask");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NonExistentTask_ReturnsExistFalse()
    {
        var taskName = $"opendsc-scheduledtask-{Guid.NewGuid():N}";

        var result = _resource.Get(new ScheduledTaskSchema { TaskName = taskName });

        result.Exist.Should().BeFalse();
        result.TaskName.Should().Be(taskName);
    }

    [RequiresAdminFact]
    public void Set_NewTask_CreatesTask()
    {
        var taskName = $"opendsc-scheduledtask-{Guid.NewGuid():N}";
        var expectedDescription = "OpenDSC ScheduledTask test";

        try
        {
            _resource.Set(BuildBaseSchema(taskName, expectedDescription));

            var actual = _resource.Get(new ScheduledTaskSchema { TaskName = taskName });

            actual.Exist.Should().NotBe(false);
            actual.TaskName.Should().Be(taskName);
            actual.Description.Should().Be(expectedDescription);
            actual.Triggers.Should().NotBeNull();
            actual.Actions.Should().NotBeNull();
            actual.Actions!.First().Path.Should().NotBeNullOrEmpty();
        }
        finally
        {
            _resource.Delete(new ScheduledTaskSchema { TaskName = taskName });
        }
    }

    [RequiresAdminFact]
    public void Set_ExistingTask_UpdatesDescription()
    {
        var taskName = $"opendsc-scheduledtask-{Guid.NewGuid():N}";
        var initialDescription = "initial";
        var updatedDescription = "updated";

        try
        {
            _resource.Set(BuildBaseSchema(taskName, initialDescription));
            _resource.Set(BuildBaseSchema(taskName, updatedDescription));

            var actual = _resource.Get(new ScheduledTaskSchema { TaskName = taskName });

            actual.Exist.Should().NotBe(false);
            actual.Description.Should().Be(updatedDescription);
        }
        finally
        {
            _resource.Delete(new ScheduledTaskSchema { TaskName = taskName });
        }
    }

    [RequiresAdminFact]
    public void Delete_ExistingTask_RemovesTask()
    {
        var taskName = $"opendsc-scheduledtask-{Guid.NewGuid():N}";

        _resource.Set(BuildBaseSchema(taskName, "to delete"));

        _resource.Delete(new ScheduledTaskSchema { TaskName = taskName });

        var result = _resource.Get(new ScheduledTaskSchema { TaskName = taskName });

        result.Exist.Should().BeFalse();
    }

    [RequiresAdminFact]
    public void Export_NoFilter_ReturnsTasks()
    {
        var taskName = $"opendsc-scheduledtask-{Guid.NewGuid():N}";

        try
        {
            _resource.Set(BuildBaseSchema(taskName, "export test"));

            var allTasks = _resource.Export(null).ToList();

            allTasks.Should().NotBeEmpty();
            allTasks.Any(t => string.Equals(t.TaskName, taskName, StringComparison.OrdinalIgnoreCase))
                .Should().BeTrue();
        }
        finally
        {
            _resource.Delete(new ScheduledTaskSchema { TaskName = taskName });
        }
    }
}
