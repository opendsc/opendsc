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
        var task = ts.GetTask($"{instance.TaskPath.TrimEnd('\\')}{ (instance.TaskPath == Schema.DefaultTaskPath ? "" : "\\")}{instance.TaskName}");

        if (task is null)
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
        DayOfWeek[]? daysOfWeek = null;
        int? daysInterval = null;

        if (trigger is not null)
        {
            triggerType = trigger switch
            {
                TimeTrigger => TriggerType.Once,
                DailyTrigger => TriggerType.Daily,
                WeeklyTrigger => TriggerType.Weekly,
                LogonTrigger => TriggerType.AtLogon,
                BootTrigger => TriggerType.AtStartup,
                _ => null
            };

            if (trigger is TimeTrigger || trigger is DailyTrigger || trigger is WeeklyTrigger)
            {
                startTime = trigger.StartBoundary.ToString("HH:mm");
            }

            if (trigger is WeeklyTrigger weekly)
            {
                var days = new List<DayOfWeek>();
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Monday)) days.Add(DayOfWeek.Monday);
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Tuesday)) days.Add(DayOfWeek.Tuesday);
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Wednesday)) days.Add(DayOfWeek.Wednesday);
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Thursday)) days.Add(DayOfWeek.Thursday);
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Friday)) days.Add(DayOfWeek.Friday);
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Saturday)) days.Add(DayOfWeek.Saturday);
                if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Sunday)) days.Add(DayOfWeek.Sunday);
                daysOfWeek = [.. days];
            }

            if (trigger is DailyTrigger daily)
            {
                daysInterval = daily.DaysInterval;
            }
        }

        var action = task.Definition.Actions.OfType<ExecAction>().FirstOrDefault();

        var taskPath = task.Path.Replace($"\\{task.Name}", string.Empty);

        if (!string.IsNullOrEmpty(taskPath) && !taskPath.EndsWith('\\'))
        {
            taskPath += '\\';
        }

        var schema = new Schema
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
            Description = task.Definition.RegistrationInfo.Description,
            ExecutionTimeLimit = task.Definition.Settings.ExecutionTimeLimit == TimeSpan.Zero
                ? null
                : task.Definition.Settings.ExecutionTimeLimit.ToString(),
            DisallowStartIfOnBatteries = task.Definition.Settings.DisallowStartIfOnBatteries,
            StopIfGoingOnBatteries = task.Definition.Settings.StopIfGoingOnBatteries,
            WakeToRun = task.Definition.Settings.WakeToRun,
            AllowDemandStart = task.Definition.Settings.AllowDemandStart,
            AllowHardTerminate = task.Definition.Settings.AllowHardTerminate,
            MultipleInstances = task.Definition.Settings.MultipleInstances,
            Priority = task.Definition.Settings.Priority,
            RestartCount = task.Definition.Settings.RestartCount,
            RestartInterval = task.Definition.Settings.RestartInterval == TimeSpan.Zero
                ? null
                : task.Definition.Settings.RestartInterval.ToString(),
            StartWhenAvailable = task.Definition.Settings.StartWhenAvailable,
            RunOnlyIfIdle = task.Definition.Settings.RunOnlyIfIdle,
            IdleDuration = task.Definition.Settings.IdleSettings.IdleDuration == TimeSpan.Zero
                ? null
                : task.Definition.Settings.IdleSettings.IdleDuration.ToString(),
            IdleWaitTimeout = task.Definition.Settings.IdleSettings.WaitTimeout == TimeSpan.Zero
                ? null
                : task.Definition.Settings.IdleSettings.WaitTimeout.ToString(),
            IdleRestartOnIdle = task.Definition.Settings.IdleSettings.RestartOnIdle,
            IdleStopOnIdleEnd = task.Definition.Settings.IdleSettings.StopOnIdleEnd,
            Hidden = task.Definition.Settings.Hidden,
            Compatibility = task.Definition.Settings.Compatibility,
            DisallowStartOnRemoteAppSession = task.Definition.Settings.DisallowStartOnRemoteAppSession,
            LogonType = task.Definition.Principal.LogonType
        };

        if (trigger is not null && trigger.Repetition.Interval != TimeSpan.Zero)
        {
            schema.RepetitionInterval = trigger.Repetition.Interval.ToString();
            schema.RepetitionDuration = trigger.Repetition.Duration == TimeSpan.Zero
                ? null
                : trigger.Repetition.Duration.ToString();
            schema.RepetitionStopAtDurationEnd = trigger.Repetition.StopAtDurationEnd;
        }

        if (trigger is ITriggerDelay delayTrigger && delayTrigger.Delay != TimeSpan.Zero)
        {
            schema.RandomDelay = delayTrigger.Delay.ToString();
        }

        return schema;
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        using var ts = new TaskService();
        var taskFullPath = $"{instance.TaskPath.TrimEnd('\\')}{ (instance.TaskPath == Schema.DefaultTaskPath ? "" : "\\")}{instance.TaskName}";
        var task = ts.GetTask(taskFullPath);

        if (task is not null)
        {
            ts.RootFolder.DeleteTask(taskFullPath, false);
        }

        var td = ts.NewTask();

        if (!string.IsNullOrEmpty(instance.Description))
        {
            td.RegistrationInfo.Description = instance.Description;
        }

        if (instance.ExecutionTimeLimit is not null)
        {
            td.Settings.ExecutionTimeLimit = TimeSpan.Parse(instance.ExecutionTimeLimit);
        }

        if (instance.DisallowStartIfOnBatteries is not null)
        {
            td.Settings.DisallowStartIfOnBatteries = instance.DisallowStartIfOnBatteries.Value;
        }

        if (instance.StopIfGoingOnBatteries is not null)
        {
            td.Settings.StopIfGoingOnBatteries = instance.StopIfGoingOnBatteries.Value;
        }

        if (instance.WakeToRun is not null)
        {
            td.Settings.WakeToRun = instance.WakeToRun.Value;
        }

        if (instance.AllowDemandStart is not null)
        {
            td.Settings.AllowDemandStart = instance.AllowDemandStart.Value;
        }

        if (instance.AllowHardTerminate is not null)
        {
            td.Settings.AllowHardTerminate = instance.AllowHardTerminate.Value;
        }

        if (instance.MultipleInstances is not null)
        {
            td.Settings.MultipleInstances = instance.MultipleInstances.Value;
        }

        if (instance.Priority is not null)
        {
            td.Settings.Priority = instance.Priority.Value;
        }

        if (instance.RestartCount is not null)
        {
            td.Settings.RestartCount = instance.RestartCount.Value;
        }

        if (instance.RestartInterval is not null)
        {
            td.Settings.RestartInterval = TimeSpan.Parse(instance.RestartInterval);
        }

        if (instance.StartWhenAvailable is not null)
        {
            td.Settings.StartWhenAvailable = instance.StartWhenAvailable.Value;
        }

        if (instance.RunOnlyIfIdle is not null)
        {
            td.Settings.RunOnlyIfIdle = instance.RunOnlyIfIdle.Value;
        }

        if (instance.IdleDuration is not null)
        {
            td.Settings.IdleSettings.IdleDuration = TimeSpan.Parse(instance.IdleDuration);
        }

        if (instance.IdleWaitTimeout is not null)
        {
            td.Settings.IdleSettings.WaitTimeout = TimeSpan.Parse(instance.IdleWaitTimeout);
        }

        if (instance.IdleRestartOnIdle is not null)
        {
            td.Settings.IdleSettings.RestartOnIdle = instance.IdleRestartOnIdle.Value;
        }

        if (instance.IdleStopOnIdleEnd is not null)
        {
            td.Settings.IdleSettings.StopOnIdleEnd = instance.IdleStopOnIdleEnd.Value;
        }

        if (instance.Hidden is not null)
        {
            td.Settings.Hidden = instance.Hidden.Value;
        }

        if (instance.Compatibility is not null)
        {
            td.Settings.Compatibility = instance.Compatibility.Value;
        }

        if (instance.DisallowStartOnRemoteAppSession is not null)
        {
            td.Settings.DisallowStartOnRemoteAppSession = instance.DisallowStartOnRemoteAppSession.Value;
        }

        if (instance.LogonType is not null)
        {
            td.Principal.LogonType = instance.LogonType.Value;
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

            if (instance.RepetitionInterval is not null)
            {
                trigger.Repetition.Interval = TimeSpan.Parse(instance.RepetitionInterval);

                if (instance.RepetitionDuration is not null)
                {
                    trigger.Repetition.Duration = TimeSpan.Parse(instance.RepetitionDuration);
                }

                if (instance.RepetitionStopAtDurationEnd is not null)
                {
                    trigger.Repetition.StopAtDurationEnd = instance.RepetitionStopAtDurationEnd.Value;
                }
            }

            if (instance.RandomDelay is not null && trigger is ITriggerDelay delayTrigger)
            {
                delayTrigger.Delay = TimeSpan.Parse(instance.RandomDelay);
            }

            td.Triggers.Add(trigger);
        }

        if (!string.IsNullOrEmpty(instance.Execute))
        {
            using var action = new ExecAction(instance.Execute, instance.Arguments, instance.WorkingDirectory);
            td.Actions.Add(action);
        }

        td.Principal.UserId = string.IsNullOrEmpty(instance.User) ? Schema.DefaultUser : instance.User;
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
        var taskFullPath = $"{instance.TaskPath.TrimEnd('\\')}{ (instance.TaskPath == Schema.DefaultTaskPath ? "" : "\\")}{instance.TaskName}";
        var task = ts.GetTask(taskFullPath);

        if (task is not null)
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
            DayOfWeek[]? daysOfWeek = null;
            int? daysInterval = null;

            if (trigger is not null)
            {
                triggerType = trigger switch
                {
                    TimeTrigger => TriggerType.Once,
                    DailyTrigger => TriggerType.Daily,
                    WeeklyTrigger => TriggerType.Weekly,
                    LogonTrigger => TriggerType.AtLogon,
                    BootTrigger => TriggerType.AtStartup,
                    _ => null
                };

                if (trigger is TimeTrigger || trigger is DailyTrigger || trigger is WeeklyTrigger)
                {
                    startTime = trigger.StartBoundary.ToString("HH:mm");
                }

                if (trigger is WeeklyTrigger weekly)
                {
                    var days = new List<DayOfWeek>();
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Monday)) days.Add(DayOfWeek.Monday);
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Tuesday)) days.Add(DayOfWeek.Tuesday);
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Wednesday)) days.Add(DayOfWeek.Wednesday);
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Thursday)) days.Add(DayOfWeek.Thursday);
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Friday)) days.Add(DayOfWeek.Friday);
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Saturday)) days.Add(DayOfWeek.Saturday);
                    if (weekly.DaysOfWeek.HasFlag(DaysOfTheWeek.Sunday)) days.Add(DayOfWeek.Sunday);
                    daysOfWeek = [.. days];
                }

                if (trigger is DailyTrigger daily)
                {
                    daysInterval = daily.DaysInterval;
                }
            }

            var action = task.Definition.Actions.OfType<ExecAction>().FirstOrDefault();

            var schema = new Schema
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
                Description = task.Definition.RegistrationInfo.Description,
                ExecutionTimeLimit = task.Definition.Settings.ExecutionTimeLimit == TimeSpan.Zero
                    ? null
                    : task.Definition.Settings.ExecutionTimeLimit.ToString(),
                DisallowStartIfOnBatteries = task.Definition.Settings.DisallowStartIfOnBatteries,
                StopIfGoingOnBatteries = task.Definition.Settings.StopIfGoingOnBatteries,
                WakeToRun = task.Definition.Settings.WakeToRun,
                AllowDemandStart = task.Definition.Settings.AllowDemandStart,
                AllowHardTerminate = task.Definition.Settings.AllowHardTerminate,
                MultipleInstances = task.Definition.Settings.MultipleInstances,
                Priority = task.Definition.Settings.Priority,
                RestartCount = task.Definition.Settings.RestartCount,
                RestartInterval = task.Definition.Settings.RestartInterval == TimeSpan.Zero
                    ? null
                    : task.Definition.Settings.RestartInterval.ToString(),
                StartWhenAvailable = task.Definition.Settings.StartWhenAvailable,
                RunOnlyIfIdle = task.Definition.Settings.RunOnlyIfIdle,
                IdleDuration = task.Definition.Settings.IdleSettings.IdleDuration == TimeSpan.Zero
                    ? null
                    : task.Definition.Settings.IdleSettings.IdleDuration.ToString(),
                IdleWaitTimeout = task.Definition.Settings.IdleSettings.WaitTimeout == TimeSpan.Zero
                    ? null
                    : task.Definition.Settings.IdleSettings.WaitTimeout.ToString(),
                IdleRestartOnIdle = task.Definition.Settings.IdleSettings.RestartOnIdle,
                IdleStopOnIdleEnd = task.Definition.Settings.IdleSettings.StopOnIdleEnd,
                Hidden = task.Definition.Settings.Hidden,
                Compatibility = task.Definition.Settings.Compatibility,
                DisallowStartOnRemoteAppSession = task.Definition.Settings.DisallowStartOnRemoteAppSession,
                LogonType = task.Definition.Principal.LogonType
            };

            if (trigger is not null && trigger.Repetition.Interval != TimeSpan.Zero)
            {
                schema.RepetitionInterval = trigger.Repetition.Interval.ToString();
                schema.RepetitionDuration = trigger.Repetition.Duration == TimeSpan.Zero
                    ? null
                    : trigger.Repetition.Duration.ToString();
                schema.RepetitionStopAtDurationEnd = trigger.Repetition.StopAtDurationEnd;
            }

            if (trigger is ITriggerDelay delayTrigger && delayTrigger.Delay != TimeSpan.Zero)
            {
                schema.RandomDelay = delayTrigger.Delay.ToString();
            }

            yield return schema;
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

        if (instance.DaysOfWeek is not null && instance.DaysOfWeek.Length > 0)
        {
            trigger.DaysOfWeek = 0;
            foreach (var day in instance.DaysOfWeek)
            {
                trigger.DaysOfWeek |= day switch
                {
                    DayOfWeek.Monday => DaysOfTheWeek.Monday,
                    DayOfWeek.Tuesday => DaysOfTheWeek.Tuesday,
                    DayOfWeek.Wednesday => DaysOfTheWeek.Wednesday,
                    DayOfWeek.Thursday => DaysOfTheWeek.Thursday,
                    DayOfWeek.Friday => DaysOfTheWeek.Friday,
                    DayOfWeek.Saturday => DaysOfTheWeek.Saturday,
                    DayOfWeek.Sunday => DaysOfTheWeek.Sunday,
                    _ => throw new ArgumentException($"Invalid day of week: {day}")
                };
            }
        }

        return trigger;
    }

    private static TaskFolder GetOrCreateFolder(TaskService ts, string path)
    {
        if (path == Schema.DefaultTaskPath)
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
