// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.ScheduledTask;

[Title("Windows Scheduled Task Schema")]
[Description("Schema for managing Windows scheduled tasks via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The name of the scheduled task.")]
    [Pattern(@"^[^\\/:\*\?""<>\|\x00]+$")]
    public string TaskName { get; set; } = string.Empty;

    [Description("The path where the task is located in Task Scheduler. Default is root (\\).")]
    [Pattern(@"^\\([^\\]+(\\[^\\]+)*\\?)?$")]
    [Default("\\")]
    public string TaskPath { get; set; } = "\\";

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
    [Default("SYSTEM")]
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
    public DaysOfWeek[]? DaysOfWeek { get; set; }

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

    [Description("Whether to run the task only if the computer is on AC power.")]
    [Nullable(false)]
    public bool? RunOnlyIfNetworkAvailable { get; set; }

    [Description("The task description.")]
    [Nullable(false)]
    public string? Description { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the scheduled task should exist.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
