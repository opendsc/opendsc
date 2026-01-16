// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Posix.Cron.Job;

[Title("POSIX Cron Job Schema")]
[Description("Schema for managing scheduled jobs in crontab files.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("A unique name for this cron job. Used to identify and manage the job.")]
    [Pattern(@"^[a-zA-Z0-9_-]+$")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("_scope")]
    [Description("The scope of the crontab. 'User' for user crontabs, 'System' for system cron files in /etc/cron.d/.")]
    [Nullable(false)]
    [Default("User")]
    public CronScope? Scope { get; set; }

    [Description("The username for user-scoped crontabs. Defaults to current user if omitted when _scope is 'User'.")]
    [Nullable(false)]
    public string? User { get; set; }

    [Description("The filename in /etc/cron.d/ for system-scoped crontabs. Required when _scope is 'System'.")]
    [Pattern(@"^[a-zA-Z0-9_-]+$")]
    [Nullable(false)]
    public string? FileName { get; set; }

    [Required]
    [Description("The schedule in cron format (minute hour day month weekday) or special strings (@hourly, @daily, @weekly, @monthly, @yearly, @reboot).")]
    [Pattern(@"^(@reboot|@yearly|@annually|@monthly|@weekly|@daily|@hourly|([*0-9,/-]+\s+){4}[*0-9,/-]+)$")]
    public string Schedule { get; set; } = string.Empty;

    [Required]
    [Description("The command to execute.")]
    public string Command { get; set; } = string.Empty;

    [Description("The user to run the command as. Only applicable for System scope. Defaults to 'root' for system crontabs.")]
    [Nullable(false)]
    public string? RunAsUser { get; set; }

    [Description("Optional comment to add above the job entry.")]
    [Nullable(false)]
    public string? Comment { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Whether the job exists in the crontab.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
