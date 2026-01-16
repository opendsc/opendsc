// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

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
    private const string DateTimeFormat = "s";

    public override string GetSchema()
    {
        var assembly = typeof(Resource).Assembly;
        var resourceName = "OpenDsc.Resource.Windows.ScheduledTask.schema.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
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

        var triggers = new List<Trigger>();
        foreach (var trigger in task.Definition.Triggers)
        {
            var schemaTrigger = TriggerToSchema(trigger);
            if (schemaTrigger is not null)
            {
                triggers.Add(schemaTrigger);
            }
        }

        var actions = new List<Action>();
        foreach (var action in task.Definition.Actions.OfType<ExecAction>())
        {
            actions.Add(new Action
            {
                Path = action.Path,
                Arguments = action.Arguments,
                WorkingDirectory = action.WorkingDirectory
            });
        }

        var taskPath = task.Path.Replace($"\\{task.Name}", string.Empty);

        if (!string.IsNullOrEmpty(taskPath) && !taskPath.EndsWith('\\'))
        {
            taskPath += '\\';
        }

        return new Schema
        {
            TaskName = instance.TaskName,
            TaskPath = taskPath,
            Triggers = triggers.Count > 0 ? [.. triggers] : null,
            Actions = actions.Count > 0 ? [.. actions] : null,
            User = task.Definition.Principal.UserId ?? task.Definition.Principal.GroupId,
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

        if (instance.Triggers is not null)
        {
            foreach (var triggerWrapper in instance.Triggers)
            {
                Microsoft.Win32.TaskScheduler.Trigger? libTrigger = null;

                if (triggerWrapper.Time is not null)
                {
                    libTrigger = CreateTimeTrigger(triggerWrapper.Time);
                }
                else if (triggerWrapper.Daily is not null)
                {
                    libTrigger = CreateDailyTrigger(triggerWrapper.Daily);
                }
                else if (triggerWrapper.Weekly is not null)
                {
                    libTrigger = CreateWeeklyTrigger(triggerWrapper.Weekly);
                }
                else if (triggerWrapper.Boot is not null)
                {
                    libTrigger = CreateBootTrigger(triggerWrapper.Boot);
                }
                else if (triggerWrapper.Logon is not null)
                {
                    libTrigger = CreateLogonTrigger(triggerWrapper.Logon);
                }

                if (libTrigger is not null)
                {
                    td.Triggers.Add(libTrigger);
                }
            }
        }

        if (instance.Actions is not null)
        {
            foreach (var action in instance.Actions)
            {
                using var execAction = new ExecAction(action.Path, action.Arguments, action.WorkingDirectory);
                td.Actions.Add(execAction);
            }
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
            var triggers = new List<Trigger>();
            foreach (var trigger in task.Definition.Triggers)
            {
                var schemaTrigger = TriggerToSchema(trigger);
                if (schemaTrigger is not null)
                {
                    triggers.Add(schemaTrigger);
                }
            }

            var actions = new List<Action>();
            foreach (var action in task.Definition.Actions.OfType<ExecAction>())
            {
                actions.Add(new Action
                {
                    Path = action.Path,
                    Arguments = action.Arguments,
                    WorkingDirectory = action.WorkingDirectory
                });
            }

            var taskPath = task.Path.Replace($"\\{task.Name}", string.Empty);
            if (!string.IsNullOrEmpty(taskPath) && !taskPath.EndsWith('\\'))
            {
                taskPath += '\\';
            }

            yield return new Schema
            {
                TaskName = task.Name,
                TaskPath = taskPath,
                Triggers = triggers.Count > 0 ? [.. triggers] : null,
                Actions = actions.Count > 0 ? [.. actions] : null,
                User = task.Definition.Principal.UserId ?? task.Definition.Principal.GroupId,
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
        }

        foreach (var subfolder in folder.SubFolders)
        {
            foreach (var schema in ExportTasksFromFolder(subfolder))
            {
                yield return schema;
            }
        }
    }

    private static Trigger? TriggerToSchema(Microsoft.Win32.TaskScheduler.Trigger libTrigger)
    {
        object? config = libTrigger switch
        {
            TimeTrigger timeTrigger => new TimeTriggerConfig
            {
                StartBoundary = timeTrigger.StartBoundary.ToString(DateTimeFormat),
                EndBoundary = timeTrigger.EndBoundary == DateTime.MinValue ? null : timeTrigger.EndBoundary.ToString(DateTimeFormat),
                Enabled = timeTrigger.Enabled,
                ExecutionTimeLimit = timeTrigger.ExecutionTimeLimit == TimeSpan.Zero ? null : timeTrigger.ExecutionTimeLimit.ToString(),
                RepetitionInterval = timeTrigger.Repetition.Interval == TimeSpan.Zero ? null : timeTrigger.Repetition.Interval.ToString(),
                RepetitionDuration = timeTrigger.Repetition.Duration == TimeSpan.Zero ? null : timeTrigger.Repetition.Duration.ToString(),
                RepetitionStopAtDurationEnd = timeTrigger.Repetition.StopAtDurationEnd ? true : null,
                RandomDelay = (timeTrigger as ITriggerDelay)?.Delay == TimeSpan.Zero ? null : (timeTrigger as ITriggerDelay)?.Delay.ToString()
            },
            DailyTrigger dailyTrigger => new DailyTriggerConfig
            {
                StartBoundary = dailyTrigger.StartBoundary.ToString(DateTimeFormat),
                EndBoundary = dailyTrigger.EndBoundary == DateTime.MinValue ? null : dailyTrigger.EndBoundary.ToString(DateTimeFormat),
                Enabled = dailyTrigger.Enabled,
                ExecutionTimeLimit = dailyTrigger.ExecutionTimeLimit == TimeSpan.Zero ? null : dailyTrigger.ExecutionTimeLimit.ToString(),
                RepetitionInterval = dailyTrigger.Repetition.Interval == TimeSpan.Zero ? null : dailyTrigger.Repetition.Interval.ToString(),
                RepetitionDuration = dailyTrigger.Repetition.Duration == TimeSpan.Zero ? null : dailyTrigger.Repetition.Duration.ToString(),
                RepetitionStopAtDurationEnd = dailyTrigger.Repetition.StopAtDurationEnd ? true : null,
                RandomDelay = (dailyTrigger as ITriggerDelay)?.Delay == TimeSpan.Zero ? null : (dailyTrigger as ITriggerDelay)?.Delay.ToString(),
                DaysInterval = dailyTrigger.DaysInterval
            },
            WeeklyTrigger weeklyTrigger => new WeeklyTriggerConfig
            {
                StartBoundary = weeklyTrigger.StartBoundary.ToString(DateTimeFormat),
                EndBoundary = weeklyTrigger.EndBoundary == DateTime.MinValue ? null : weeklyTrigger.EndBoundary.ToString(DateTimeFormat),
                Enabled = weeklyTrigger.Enabled,
                ExecutionTimeLimit = weeklyTrigger.ExecutionTimeLimit == TimeSpan.Zero ? null : weeklyTrigger.ExecutionTimeLimit.ToString(),
                RepetitionInterval = weeklyTrigger.Repetition.Interval == TimeSpan.Zero ? null : weeklyTrigger.Repetition.Interval.ToString(),
                RepetitionDuration = weeklyTrigger.Repetition.Duration == TimeSpan.Zero ? null : weeklyTrigger.Repetition.Duration.ToString(),
                RepetitionStopAtDurationEnd = weeklyTrigger.Repetition.StopAtDurationEnd ? true : null,
                RandomDelay = (weeklyTrigger as ITriggerDelay)?.Delay == TimeSpan.Zero ? null : (weeklyTrigger as ITriggerDelay)?.Delay.ToString(),
                DaysOfWeek = ConvertDaysOfTheWeekToDayOfWeekArray(weeklyTrigger.DaysOfWeek),
                WeeksInterval = weeklyTrigger.WeeksInterval
            },
            BootTrigger bootTrigger => new BootTriggerConfig
            {
                EndBoundary = bootTrigger.EndBoundary == DateTime.MinValue ? null : bootTrigger.EndBoundary.ToString(DateTimeFormat),
                Enabled = bootTrigger.Enabled,
                ExecutionTimeLimit = bootTrigger.ExecutionTimeLimit == TimeSpan.Zero ? null : bootTrigger.ExecutionTimeLimit.ToString(),
                RepetitionInterval = bootTrigger.Repetition.Interval == TimeSpan.Zero ? null : bootTrigger.Repetition.Interval.ToString(),
                RepetitionDuration = bootTrigger.Repetition.Duration == TimeSpan.Zero ? null : bootTrigger.Repetition.Duration.ToString(),
                RepetitionStopAtDurationEnd = bootTrigger.Repetition.StopAtDurationEnd ? true : null,
                RandomDelay = bootTrigger.Delay == TimeSpan.Zero ? null : bootTrigger.Delay.ToString()
            },
            LogonTrigger logonTrigger => new LogonTriggerConfig
            {
                EndBoundary = logonTrigger.EndBoundary == DateTime.MinValue ? null : logonTrigger.EndBoundary.ToString(DateTimeFormat),
                Enabled = logonTrigger.Enabled,
                ExecutionTimeLimit = logonTrigger.ExecutionTimeLimit == TimeSpan.Zero ? null : logonTrigger.ExecutionTimeLimit.ToString(),
                RepetitionInterval = logonTrigger.Repetition.Interval == TimeSpan.Zero ? null : logonTrigger.Repetition.Interval.ToString(),
                RepetitionDuration = logonTrigger.Repetition.Duration == TimeSpan.Zero ? null : logonTrigger.Repetition.Duration.ToString(),
                RepetitionStopAtDurationEnd = logonTrigger.Repetition.StopAtDurationEnd ? true : null,
                RandomDelay = logonTrigger.Delay == TimeSpan.Zero ? null : logonTrigger.Delay.ToString(),
                UserId = logonTrigger.UserId
            },
            _ => null
        };

        if (config is null)
        {
            return null;
        }

        return config switch
        {
            TimeTriggerConfig time => new Trigger { Time = time },
            DailyTriggerConfig daily => new Trigger { Daily = daily },
            WeeklyTriggerConfig weekly => new Trigger { Weekly = weekly },
            BootTriggerConfig boot => new Trigger { Boot = boot },
            LogonTriggerConfig logon => new Trigger { Logon = logon },
            _ => null
        };
    }

    private static DayOfWeek[] ConvertDaysOfTheWeekToDayOfWeekArray(DaysOfTheWeek daysOfWeek)
    {
        var days = new List<DayOfWeek>();
        if (daysOfWeek.HasFlag(DaysOfTheWeek.Monday)) days.Add(DayOfWeek.Monday);
        if (daysOfWeek.HasFlag(DaysOfTheWeek.Tuesday)) days.Add(DayOfWeek.Tuesday);
        if (daysOfWeek.HasFlag(DaysOfTheWeek.Wednesday)) days.Add(DayOfWeek.Wednesday);
        if (daysOfWeek.HasFlag(DaysOfTheWeek.Thursday)) days.Add(DayOfWeek.Thursday);
        if (daysOfWeek.HasFlag(DaysOfTheWeek.Friday)) days.Add(DayOfWeek.Friday);
        if (daysOfWeek.HasFlag(DaysOfTheWeek.Saturday)) days.Add(DayOfWeek.Saturday);
        if (daysOfWeek.HasFlag(DaysOfTheWeek.Sunday)) days.Add(DayOfWeek.Sunday);
        return [.. days];
    }

    private static TimeTrigger CreateTimeTrigger(TimeTriggerConfig config)
    {
        var trigger = new TimeTrigger
        {
            StartBoundary = DateTime.Parse(config.StartBoundary)
        };

        SetCommonTriggerProperties(trigger, config.EndBoundary, config.Enabled, config.ExecutionTimeLimit,
            config.RepetitionInterval, config.RepetitionDuration, config.RepetitionStopAtDurationEnd, config.RandomDelay);

        return trigger;
    }

    private static DailyTrigger CreateDailyTrigger(DailyTriggerConfig config)
    {
        var trigger = new DailyTrigger
        {
            StartBoundary = DateTime.Parse(config.StartBoundary),
            DaysInterval = (short)(config.DaysInterval ?? 1)
        };

        SetCommonTriggerProperties(trigger, config.EndBoundary, config.Enabled, config.ExecutionTimeLimit,
            config.RepetitionInterval, config.RepetitionDuration, config.RepetitionStopAtDurationEnd, config.RandomDelay);

        return trigger;
    }

    private static WeeklyTrigger CreateWeeklyTrigger(WeeklyTriggerConfig config)
    {
        var trigger = new WeeklyTrigger
        {
            StartBoundary = DateTime.Parse(config.StartBoundary),
            WeeksInterval = (short)(config.WeeksInterval ?? 1)
        };

        if (config.DaysOfWeek is not null && config.DaysOfWeek.Length > 0)
        {
            trigger.DaysOfWeek = 0;
            foreach (var day in config.DaysOfWeek)
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

        SetCommonTriggerProperties(trigger, config.EndBoundary, config.Enabled, config.ExecutionTimeLimit,
            config.RepetitionInterval, config.RepetitionDuration, config.RepetitionStopAtDurationEnd, config.RandomDelay);

        return trigger;
    }

    private static BootTrigger CreateBootTrigger(BootTriggerConfig config)
    {
        var trigger = new BootTrigger();

        SetCommonTriggerProperties(trigger, config.EndBoundary, config.Enabled, config.ExecutionTimeLimit,
            config.RepetitionInterval, config.RepetitionDuration, config.RepetitionStopAtDurationEnd, config.RandomDelay);

        return trigger;
    }

    private static LogonTrigger CreateLogonTrigger(LogonTriggerConfig config)
    {
        var trigger = new LogonTrigger();

        if (!string.IsNullOrEmpty(config.UserId))
        {
            trigger.UserId = config.UserId;
        }

        SetCommonTriggerProperties(trigger, config.EndBoundary, config.Enabled, config.ExecutionTimeLimit,
            config.RepetitionInterval, config.RepetitionDuration, config.RepetitionStopAtDurationEnd, config.RandomDelay);

        return trigger;
    }

    private static void SetCommonTriggerProperties(Microsoft.Win32.TaskScheduler.Trigger trigger, string? endBoundary,
        bool? enabled, string? executionTimeLimit, string? repetitionInterval, string? repetitionDuration,
        bool? repetitionStopAtDurationEnd, string? randomDelay)
    {
        if (endBoundary is not null)
        {
            trigger.EndBoundary = DateTime.Parse(endBoundary);
        }

        if (enabled is not null)
        {
            trigger.Enabled = enabled.Value;
        }

        if (executionTimeLimit is not null)
        {
            trigger.ExecutionTimeLimit = TimeSpan.Parse(executionTimeLimit);
        }

        if (repetitionInterval is not null)
        {
            trigger.Repetition.Interval = TimeSpan.Parse(repetitionInterval);

            if (repetitionDuration is not null)
            {
                trigger.Repetition.Duration = TimeSpan.Parse(repetitionDuration);
            }

            if (repetitionStopAtDurationEnd is not null)
            {
                trigger.Repetition.StopAtDurationEnd = repetitionStopAtDurationEnd.Value;
            }
        }

        if (randomDelay is not null && trigger is ITriggerDelay delayTrigger)
        {
            delayTrigger.Delay = TimeSpan.Parse(randomDelay);
        }
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
