// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using Microsoft.Win32.TaskScheduler;

namespace OpenDsc.Resource.Windows.ScheduledTask;

[DscResource("OpenDsc.Windows/ScheduledTask", "0.1.0", Description = "Manage Windows scheduled tasks", Tags = ["windows", "scheduled", "task", "scheduler"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(SecurityException), Description = "Access denied")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>, IExportable<Schema>
{
    public override string GetSchema()
    {
        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase
        };

        var builder = new JsonSchemaBuilder().FromType<Schema>(config);
        builder.Schema("https://json-schema.org/draft/2020-12/schema");
        var schema = builder.Build();

        return JsonSerializer.Serialize(schema);
    }

    public Schema Get(Schema instance)
    {
        using var ts = new TaskService();
        var task = ts.GetTask($"{instance.TaskPath.TrimEnd('\\')}{(instance.TaskPath == "\\" ? "" : "\\")}{instance.TaskName}");

        if (task == null)
        {
            return new Schema
            {
                TaskName = instance.TaskName,
                TaskPath = instance.TaskPath,
                Exist = false
            };
        }

        var trigger = task.Definition.Triggers.FirstOrDefault();
        TriggerType? triggerType = null;
        string? startTime = null;
        DaysOfWeek[]? daysOfWeek = null;
        int? daysInterval = null;

        if (trigger != null)
        {
            triggerType = trigger switch
            {
                TimeTrigger => TriggerType.Once,
                DailyTrigger => TriggerType.Daily,
                WeeklyTrigger => TriggerType.Weekly,
                LogonTrigger => TriggerType.AtLogon,
                BootTrigger => TriggerType.AtStartup,
                _ => (TriggerType?)null
            };

            if (trigger is TimeTrigger || trigger is DailyTrigger || trigger is WeeklyTrigger)
            {
                startTime = trigger.StartBoundary.ToString("HH:mm");
            }

            if (trigger is WeeklyTrigger weekly)
            {
                var days = new List<DaysOfWeek>();
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Monday)) days.Add(DaysOfWeek.Monday);
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Tuesday)) days.Add(DaysOfWeek.Tuesday);
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Wednesday)) days.Add(DaysOfWeek.Wednesday);
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Thursday)) days.Add(DaysOfWeek.Thursday);
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Friday)) days.Add(DaysOfWeek.Friday);
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Saturday)) days.Add(DaysOfWeek.Saturday);
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Sunday)) days.Add(DaysOfWeek.Sunday);
                daysOfWeek = [.. days];
            }

            if (trigger is DailyTrigger daily)
            {
                daysInterval = daily.DaysInterval;
            }
        }

        var action = task.Definition.Actions.OfType<ExecAction>().FirstOrDefault();

        var taskPath = task.Path.Replace($"\\{task.Name}", string.Empty);
        // Ensure task path ends with backslash unless it's the root
        if (!string.IsNullOrEmpty(taskPath) && !taskPath.EndsWith('\\'))
        {
            taskPath += '\\';
        }

        return new Schema
        {
            TaskName = instance.TaskName,
            TaskPath = taskPath,
            Execute = action?.Path,
            Arguments = action?.Arguments,
            WorkingDirectory = action?.WorkingDirectory,
            User = task.Definition.Principal.UserId ?? task.Definition.Principal.GroupId,
            TriggerType = triggerType,
            StartTime = startTime,
            DaysOfWeek = daysOfWeek,
            DaysInterval = daysInterval,
            Enabled = task.Enabled,
            RunWithHighestPrivileges = task.Definition.Principal.RunLevel == TaskRunLevel.Highest,
            RunOnlyIfNetworkAvailable = task.Definition.Settings.RunOnlyIfNetworkAvailable,
            Description = task.Definition.RegistrationInfo.Description
        };
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        using var ts = new TaskService();
        var taskFullPath = $"{instance.TaskPath.TrimEnd('\\')}{(instance.TaskPath == "\\" ? "" : "\\")}{instance.TaskName}";
        var task = ts.GetTask(taskFullPath);

        if (task != null)
        {
            ts.RootFolder.DeleteTask(taskFullPath, false);
        }

        var td = ts.NewTask();

        if (!string.IsNullOrEmpty(instance.Description))
        {
            td.RegistrationInfo.Description = instance.Description;
        }

        if (instance.TriggerType.HasValue)
        {
            Trigger trigger = instance.TriggerType.Value switch
            {
                TriggerType.Once => CreateTimeTrigger(instance),
                TriggerType.Daily => CreateDailyTrigger(instance),
                TriggerType.Weekly => CreateWeeklyTrigger(instance),
                TriggerType.AtLogon => new LogonTrigger(),
                TriggerType.AtStartup => new BootTrigger(),
                _ => throw new ArgumentException($"Unsupported trigger type: {instance.TriggerType}")
            };
            td.Triggers.Add(trigger);
        }

        if (!string.IsNullOrEmpty(instance.Execute))
        {
            var action = new ExecAction(instance.Execute, instance.Arguments, instance.WorkingDirectory);
            td.Actions.Add(action);
        }

        td.Principal.UserId = string.IsNullOrEmpty(instance.User) ? "SYSTEM" : instance.User;
        td.Principal.RunLevel = instance.RunWithHighestPrivileges == true ? TaskRunLevel.Highest : TaskRunLevel.LUA;
        td.Settings.Enabled = instance.Enabled ?? true;
        td.Settings.RunOnlyIfNetworkAvailable = instance.RunOnlyIfNetworkAvailable ?? false;

        var folder = GetOrCreateFolder(ts, instance.TaskPath);
        folder.RegisterTaskDefinition(instance.TaskName, td);

        return null;
    }

    public void Delete(Schema instance)
    {
        using var ts = new TaskService();
        var taskFullPath = $"{instance.TaskPath.TrimEnd('\\')}{(instance.TaskPath == "\\" ? "" : "\\")}{instance.TaskName}";
        var task = ts.GetTask(taskFullPath);

        if (task != null)
        {
            ts.RootFolder.DeleteTask(taskFullPath, false);
        }
    }

    public IEnumerable<Schema> Export()
    {
        using var ts = new TaskService();
        return ExportTasksFromFolder(ts.RootFolder);
    }

    private static IEnumerable<Schema> ExportTasksFromFolder(TaskFolder folder)
    {
        foreach (var task in folder.Tasks)
        {
            var trigger = task.Definition.Triggers.FirstOrDefault();
            TriggerType? triggerType = null;
            string? startTime = null;
            DaysOfWeek[]? daysOfWeek = null;
            int? daysInterval = null;

            if (trigger != null)
            {
                triggerType = trigger switch
                {
                    TimeTrigger => TriggerType.Once,
                    DailyTrigger => TriggerType.Daily,
                    WeeklyTrigger => TriggerType.Weekly,
                    LogonTrigger => TriggerType.AtLogon,
                    BootTrigger => TriggerType.AtStartup,
                    _ => (TriggerType?)null
                };

                if (trigger is TimeTrigger || trigger is DailyTrigger || trigger is WeeklyTrigger)
                {
                    startTime = trigger.StartBoundary.ToString("HH:mm");
                }

                if (trigger is WeeklyTrigger weekly)
                {
                    var days = new List<DaysOfWeek>();
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Monday)) days.Add(DaysOfWeek.Monday);
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Tuesday)) days.Add(DaysOfWeek.Tuesday);
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Wednesday)) days.Add(DaysOfWeek.Wednesday);
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Thursday)) days.Add(DaysOfWeek.Thursday);
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Friday)) days.Add(DaysOfWeek.Friday);
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Saturday)) days.Add(DaysOfWeek.Saturday);
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Sunday)) days.Add(DaysOfWeek.Sunday);
                    daysOfWeek = [.. days];
                }

                if (trigger is DailyTrigger daily)
                {
                    daysInterval = daily.DaysInterval;
                }
            }

            var action = task.Definition.Actions.OfType<ExecAction>().FirstOrDefault();

            yield return new Schema
            {
                TaskName = task.Name,
                TaskPath = task.Path.Replace(task.Name, string.Empty),
                Execute = action?.Path,
                Arguments = action?.Arguments,
                WorkingDirectory = action?.WorkingDirectory,
                User = task.Definition.Principal.UserId ?? task.Definition.Principal.GroupId,
                TriggerType = triggerType,
                StartTime = startTime,
                DaysOfWeek = daysOfWeek,
                DaysInterval = daysInterval,
                Enabled = task.Enabled,
                RunWithHighestPrivileges = task.Definition.Principal.RunLevel == TaskRunLevel.Highest,
                RunOnlyIfNetworkAvailable = task.Definition.Settings.RunOnlyIfNetworkAvailable,
                Description = task.Definition.RegistrationInfo.Description
            };
        }

        foreach (var subfolder in folder.SubFolders)
        {
            foreach (var schema in ExportTasksFromFolder(subfolder))
            {
                yield return schema;
            }
        }
    }

    private static TimeTrigger CreateTimeTrigger(Schema instance)
    {
        var trigger = new TimeTrigger();
        if (!string.IsNullOrEmpty(instance.StartTime))
        {
            var parts = instance.StartTime.Split(':');
            var now = DateTime.Now;
            trigger.StartBoundary = new DateTime(now.Year, now.Month, now.Day, int.Parse(parts[0]), int.Parse(parts[1]), 0);
        }
        return trigger;
    }

    private static DailyTrigger CreateDailyTrigger(Schema instance)
    {
        var trigger = new DailyTrigger { DaysInterval = (short)(instance.DaysInterval ?? 1) };
        if (!string.IsNullOrEmpty(instance.StartTime))
        {
            var parts = instance.StartTime.Split(':');
            var now = DateTime.Now;
            trigger.StartBoundary = new DateTime(now.Year, now.Month, now.Day, int.Parse(parts[0]), int.Parse(parts[1]), 0);
        }
        return trigger;
    }

    private static WeeklyTrigger CreateWeeklyTrigger(Schema instance)
    {
        var trigger = new WeeklyTrigger();
        if (!string.IsNullOrEmpty(instance.StartTime))
        {
            var parts = instance.StartTime.Split(':');
            var now = DateTime.Now;
            trigger.StartBoundary = new DateTime(now.Year, now.Month, now.Day, int.Parse(parts[0]), int.Parse(parts[1]), 0);
        }

        if (instance.DaysOfWeek != null && instance.DaysOfWeek.Length > 0)
        {
            trigger.DaysOfWeek = 0;
            foreach (var day in instance.DaysOfWeek)
            {
                trigger.DaysOfWeek |= day switch
                {
                    DaysOfWeek.Monday => DaysOfTheWeek.Monday,
                    DaysOfWeek.Tuesday => DaysOfTheWeek.Tuesday,
                    DaysOfWeek.Wednesday => DaysOfTheWeek.Wednesday,
                    DaysOfWeek.Thursday => DaysOfTheWeek.Thursday,
                    DaysOfWeek.Friday => DaysOfTheWeek.Friday,
                    DaysOfWeek.Saturday => DaysOfTheWeek.Saturday,
                    DaysOfWeek.Sunday => DaysOfTheWeek.Sunday,
                    _ => throw new ArgumentException($"Invalid day of week: {day}")
                };
            }
        }

        return trigger;
    }

    private static TaskFolder GetOrCreateFolder(TaskService ts, string path)
    {
        if (path == "\\")
        {
            return ts.RootFolder;
        }

        var parts = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        TaskFolder folder = ts.RootFolder;

        foreach (var part in parts)
        {
            try
            {
                folder = folder.SubFolders[part];
            }
            catch
            {
                folder = folder.CreateFolder(part);
            }
        }

        return folder;
    }
}
