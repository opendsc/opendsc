// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.ScheduledTask;

[Title("Scheduled Task Trigger")]
[Description("Defines when a scheduled task should run.")]
[AdditionalProperties(false)]
public sealed class Trigger
{
    [Description("Run once at a specific time.")]
    [Nullable(false)]
    public TimeTriggerConfig? Time { get; set; }

    [Description("Run daily at specified intervals.")]
    [Nullable(false)]
    public DailyTriggerConfig? Daily { get; set; }

    [Description("Run weekly on specific days.")]
    [Nullable(false)]
    public WeeklyTriggerConfig? Weekly { get; set; }

    [Description("Run when the system boots.")]
    [Nullable(false)]
    public BootTriggerConfig? Boot { get; set; }

    [Description("Run when a user logs on.")]
    [Nullable(false)]
    public LogonTriggerConfig? Logon { get; set; }
}

[Title("Time Trigger Configuration")]
[Description("Configuration for running a task once at a specific time.")]
[AdditionalProperties(false)]
public sealed class TimeTriggerConfig
{
    [Required]
    [Description("The date and time when the trigger activates. Format: yyyy-MM-ddTHH:mm:ss (e.g., '2026-01-15T09:00:00').")]
    [Pattern(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$")]
    public string StartBoundary { get; set; } = string.Empty;

    [Description("The date and time when the trigger deactivates. Format: yyyy-MM-ddTHH:mm:ss.")]
    [Pattern(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$")]
    [Nullable(false)]
    public string? EndBoundary { get; set; }

    [Description("Whether the trigger is enabled.")]
    [Default(true)]
    [Nullable(false)]
    public bool? Enabled { get; set; }

    [Description("Maximum time the task can run after being triggered. Format: TimeSpan (e.g., '01:00:00' for 1 hour). Overrides task-level setting.")]
    [Nullable(false)]
    public string? ExecutionTimeLimit { get; set; }

    [Description("Time between task repetitions. Minimum: 1 minute. Format: TimeSpan (e.g., '00:10:00' for 10 minutes).")]
    [Nullable(false)]
    public string? RepetitionInterval { get; set; }

    [Description("How long to repeat the task. Zero ('00:00:00') for infinite. Format: TimeSpan.")]
    [Nullable(false)]
    public string? RepetitionDuration { get; set; }

    [Description("Stop running task instances at duration end.")]
    [Nullable(false)]
    public bool? RepetitionStopAtDurationEnd { get; set; }

    [Description("Random delay before task starts. Format: TimeSpan (e.g., '00:05:00' for 5 minutes).")]
    [Nullable(false)]
    public string? RandomDelay { get; set; }
}

[Title("Daily Trigger Configuration")]
[Description("Configuration for running a task daily at specified intervals.")]
[AdditionalProperties(false)]
public sealed class DailyTriggerConfig
{
    [Required]
    [Description("The date and time when the trigger activates. Format: yyyy-MM-ddTHH:mm:ss (e.g., '2026-01-15T09:00:00').")]
    [Pattern(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$")]
    public string StartBoundary { get; set; } = string.Empty;

    [Description("The date and time when the trigger deactivates. Format: yyyy-MM-ddTHH:mm:ss.")]
    [Pattern(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$")]
    [Nullable(false)]
    public string? EndBoundary { get; set; }

    [Description("Whether the trigger is enabled.")]
    [Default(true)]
    [Nullable(false)]
    public bool? Enabled { get; set; }

    [Description("Maximum time the task can run after being triggered. Format: TimeSpan (e.g., '01:00:00' for 1 hour). Overrides task-level setting.")]
    [Nullable(false)]
    public string? ExecutionTimeLimit { get; set; }

    [Description("Time between task repetitions. Minimum: 1 minute. Format: TimeSpan (e.g., '00:10:00' for 10 minutes).")]
    [Nullable(false)]
    public string? RepetitionInterval { get; set; }

    [Description("How long to repeat the task. Zero ('00:00:00') for infinite. Format: TimeSpan.")]
    [Nullable(false)]
    public string? RepetitionDuration { get; set; }

    [Description("Stop running task instances at duration end.")]
    [Nullable(false)]
    public bool? RepetitionStopAtDurationEnd { get; set; }

    [Description("Random delay before task starts. Format: TimeSpan (e.g., '00:05:00' for 5 minutes).")]
    [Nullable(false)]
    public string? RandomDelay { get; set; }

    [Description("Interval in days between task runs. Default is 1 (run every day).")]
    [Minimum(1)]
    [Nullable(false)]
    public int? DaysInterval { get; set; }
}

[Title("Weekly Trigger Configuration")]
[Description("Configuration for running a task weekly on specific days.")]
[AdditionalProperties(false)]
public sealed class WeeklyTriggerConfig
{
    [Required]
    [Description("The date and time when the trigger activates. Format: yyyy-MM-ddTHH:mm:ss (e.g., '2026-01-15T09:00:00').")]
    [Pattern(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$")]
    public string StartBoundary { get; set; } = string.Empty;

    [Description("The date and time when the trigger deactivates. Format: yyyy-MM-ddTHH:mm:ss.")]
    [Pattern(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$")]
    [Nullable(false)]
    public string? EndBoundary { get; set; }

    [Description("Whether the trigger is enabled.")]
    [Default(true)]
    [Nullable(false)]
    public bool? Enabled { get; set; }

    [Description("Maximum time the task can run after being triggered. Format: TimeSpan (e.g., '01:00:00' for 1 hour). Overrides task-level setting.")]
    [Nullable(false)]
    public string? ExecutionTimeLimit { get; set; }

    [Description("Time between task repetitions. Minimum: 1 minute. Format: TimeSpan (e.g., '00:10:00' for 10 minutes).")]
    [Nullable(false)]
    public string? RepetitionInterval { get; set; }

    [Description("How long to repeat the task. Zero ('00:00:00') for infinite. Format: TimeSpan.")]
    [Nullable(false)]
    public string? RepetitionDuration { get; set; }

    [Description("Stop running task instances at duration end.")]
    [Nullable(false)]
    public bool? RepetitionStopAtDurationEnd { get; set; }

    [Description("Random delay before task starts. Format: TimeSpan (e.g., '00:05:00' for 5 minutes).")]
    [Nullable(false)]
    public string? RandomDelay { get; set; }

    [Required]
    [Description("Days of the week when the task runs.")]
    [UniqueItems(true)]
    public DayOfWeek[] DaysOfWeek { get; set; } = [];

    [Description("Interval in weeks between task runs. Default is 1 (run every week).")]
    [Minimum(1)]
    [Nullable(false)]
    public int? WeeksInterval { get; set; }
}

[Title("Boot Trigger Configuration")]
[Description("Configuration for running a task when the system boots.")]
[AdditionalProperties(false)]
public sealed class BootTriggerConfig
{
    [Description("The date and time when the trigger deactivates. Format: yyyy-MM-ddTHH:mm:ss.")]
    [Pattern(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$")]
    [Nullable(false)]
    public string? EndBoundary { get; set; }

    [Description("Whether the trigger is enabled.")]
    [Default(true)]
    [Nullable(false)]
    public bool? Enabled { get; set; }

    [Description("Maximum time the task can run after being triggered. Format: TimeSpan (e.g., '01:00:00' for 1 hour). Overrides task-level setting.")]
    [Nullable(false)]
    public string? ExecutionTimeLimit { get; set; }

    [Description("Time between task repetitions. Minimum: 1 minute. Format: TimeSpan (e.g., '00:10:00' for 10 minutes).")]
    [Nullable(false)]
    public string? RepetitionInterval { get; set; }

    [Description("How long to repeat the task. Zero ('00:00:00') for infinite. Format: TimeSpan.")]
    [Nullable(false)]
    public string? RepetitionDuration { get; set; }

    [Description("Stop running task instances at duration end.")]
    [Nullable(false)]
    public bool? RepetitionStopAtDurationEnd { get; set; }

    [Description("Delay after boot before task starts. Format: TimeSpan (e.g., '00:02:00' for 2 minutes).")]
    [Nullable(false)]
    public string? RandomDelay { get; set; }
}

[Title("Logon Trigger Configuration")]
[Description("Configuration for running a task when a user logs on.")]
[AdditionalProperties(false)]
public sealed class LogonTriggerConfig
{
    [Description("The date and time when the trigger deactivates. Format: yyyy-MM-ddTHH:mm:ss.")]
    [Pattern(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$")]
    [Nullable(false)]
    public string? EndBoundary { get; set; }

    [Description("Whether the trigger is enabled.")]
    [Default(true)]
    [Nullable(false)]
    public bool? Enabled { get; set; }

    [Description("Maximum time the task can run after being triggered. Format: TimeSpan (e.g., '01:00:00' for 1 hour). Overrides task-level setting.")]
    [Nullable(false)]
    public string? ExecutionTimeLimit { get; set; }

    [Description("Time between task repetitions. Minimum: 1 minute. Format: TimeSpan (e.g., '00:10:00' for 10 minutes).")]
    [Nullable(false)]
    public string? RepetitionInterval { get; set; }

    [Description("How long to repeat the task. Zero ('00:00:00') for infinite. Format: TimeSpan.")]
    [Nullable(false)]
    public string? RepetitionDuration { get; set; }

    [Description("Stop running task instances at duration end.")]
    [Nullable(false)]
    public bool? RepetitionStopAtDurationEnd { get; set; }

    [Description("Delay after logon before task starts. Format: TimeSpan (e.g., '00:01:00' for 1 minute).")]
    [Nullable(false)]
    public string? RandomDelay { get; set; }

    [Description("The user account that triggers the task when logged on. If null, any user logon triggers the task.")]
    [Nullable(false)]
    public string? UserId { get; set; }
}
