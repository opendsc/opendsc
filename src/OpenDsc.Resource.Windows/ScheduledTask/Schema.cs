// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;

using Json.Schema.Generation;

using Microsoft.Win32.TaskScheduler;

namespace OpenDsc.Resource.Windows.ScheduledTask;

[Title("Windows Scheduled Task Schema")]
[Description("Schema for managing Windows scheduled tasks via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    public const string DefaultTaskPath = "\\";
    public const string DefaultUser = "SYSTEM";

    [Required]
    [Description("The name of the scheduled task.")]
    [Pattern(@"^[^\\/:\*\?""<>\|\x00]+$")]
    public string TaskName { get; set; } = string.Empty;

    [Description("The path where the task is located in Task Scheduler. Default is root (\\).")]
    [Pattern(@"^\\([^\\]+(\\[^\\]+)*\\?)?$")]
    [Default(DefaultTaskPath)]
    public string TaskPath { get; set; } = DefaultTaskPath;

    [Description("The executable or script to run.")]
    [Nullable(false)]
    public string? Execute { get; set; }

    [Description("The arguments to pass to the executable.")]
    [Nullable(false)]
    public string? Arguments { get; set; }

    [Description("The working directory for the task.")]
    [Nullable(false)]
    public string? WorkingDirectory { get; set; }

    [Description("The user account to run the task under. Default is SYSTEM.")]
    [Nullable(false)]
    [Default(DefaultUser)]
    public string? User { get; set; }

    [Description("The trigger schedule for the task (e.g., 'Daily', 'Weekly', 'AtLogon', 'AtStartup').")]
    [Nullable(false)]
    public TriggerType? TriggerType { get; set; }

    [Description("The time to run the task (for time-based triggers, format: HH:mm).")]
    [Nullable(false)]
    [Pattern(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$")]
    public string? StartTime { get; set; }

    [Description("Days of the week for weekly triggers.")]
    [UniqueItems(true)]
    [Nullable(false)]
    public DayOfWeek[]? DaysOfWeek { get; set; }

    [Description("Interval in days for daily triggers.")]
    [Nullable(false)]
    [Minimum(1)]
    public int? DaysInterval { get; set; }

    [Description("Whether the task is enabled.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Enabled { get; set; }

    [Description("Whether to run the task with highest privileges.")]
    [Nullable(false)]
    public bool? RunWithHighestPrivileges { get; set; }

    [Description("Whether to run the task only if a network is available.")]
    [Nullable(false)]
    public bool? RunOnlyIfNetworkAvailable { get; set; }

    [Description("The task description.")]
    [Nullable(false)]
    public string? Description { get; set; }

    [Description("Time between task repetitions. Minimum: 1 minute. Format: TimeSpan (e.g., '00:10:00' for 10 minutes).")]
    [Nullable(false)]
    public string? RepetitionInterval { get; set; }

    [Description("How long to repeat the task. Zero for infinite. Format: TimeSpan (e.g., '01:00:00' for 1 hour, '00:00:00' for infinite).")]
    [Nullable(false)]
    public string? RepetitionDuration { get; set; }

    [Description("Stop running task instances at duration end.")]
    [Nullable(false)]
    public bool? RepetitionStopAtDurationEnd { get; set; }

    [Description("Random delay before task starts. Format: TimeSpan (e.g., '00:05:00' for 5 minutes).")]
    [Nullable(false)]
    public string? RandomDelay { get; set; }

    [Description("Maximum time allowed to complete the task. Default: 3 days ('3.00:00:00'). Zero ('00:00:00') for no limit. Format: TimeSpan.")]
    [Nullable(false)]
    public string? ExecutionTimeLimit { get; set; }

    [Description("Do not start the task if the computer is running on batteries.")]
    [Default(true)]
    [Nullable(false)]
    public bool? DisallowStartIfOnBatteries { get; set; }

    [Description("Stop the task if the computer switches to battery power.")]
    [Default(true)]
    [Nullable(false)]
    public bool? StopIfGoingOnBatteries { get; set; }

    [Description("Wake the computer to run this task.")]
    [Nullable(false)]
    public bool? WakeToRun { get; set; }

    [Description("Allow the task to be run on demand.")]
    [Default(true)]
    [Nullable(false)]
    public bool? AllowDemandStart { get; set; }

    [Description("Allow the task to be terminated using TerminateProcess.")]
    [Default(true)]
    [Nullable(false)]
    public bool? AllowHardTerminate { get; set; }

    [Description("Policy for handling multiple instances of the task.")]
    [Nullable(false)]
    public TaskInstancesPolicy? MultipleInstances { get; set; }

    [Description("Task process priority.")]
    [Nullable(false)]
    public ProcessPriorityClass? Priority { get; set; }

    [Description("Number of times to attempt to restart the task on failure.")]
    [Default(0)]
    [Nullable(false)]
    [Minimum(0)]
    public int? RestartCount { get; set; }

    [Description("Time interval between task restart attempts. Format: TimeSpan (e.g., '00:01:00' for 1 minute).")]
    [Nullable(false)]
    public string? RestartInterval { get; set; }

    [Description("Start the task as soon as possible after a scheduled start is missed.")]
    [Nullable(false)]
    public bool? StartWhenAvailable { get; set; }

    [Description("Run the task only if the computer is idle.")]
    [Nullable(false)]
    public bool? RunOnlyIfIdle { get; set; }

    [Description("Computer must be idle for this duration before the task runs. Format: TimeSpan (e.g., '00:10:00' for 10 minutes).")]
    [Nullable(false)]
    public string? IdleDuration { get; set; }

    [Description("Maximum time to wait for an idle condition. Format: TimeSpan (e.g., '01:00:00' for 1 hour).")]
    [Nullable(false)]
    public string? IdleWaitTimeout { get; set; }

    [Description("Restart the task if the idle condition resumes.")]
    [Nullable(false)]
    public bool? IdleRestartOnIdle { get; set; }

    [Description("Stop the task when the computer is no longer idle.")]
    [Default(true)]
    [Nullable(false)]
    public bool? IdleStopOnIdleEnd { get; set; }

    [Description("Hide the task in the Task Scheduler UI.")]
    [Nullable(false)]
    public bool? Hidden { get; set; }

    [Description("Task Scheduler compatibility level.")]
    [Nullable(false)]
    public TaskCompatibility? Compatibility { get; set; }

    [Description("Do not start the task in a Remote Desktop session (requires V2_1 or later).")]
    [Nullable(false)]
    public bool? DisallowStartOnRemoteAppSession { get; set; }

    [Description("The security logon method for the task.")]
    [Nullable(false)]
    public TaskLogonType? LogonType { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the scheduled task should exist.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
