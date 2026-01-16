// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Posix.Cron.Environment;

[Title("POSIX Cron Environment Schema")]
[Description("Schema for managing environment variables in crontab files.")]
[AdditionalProperties(false)]
public sealed class Schema
{
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

    [Description("Environment variables to set at the top of the crontab file. Common variables: SHELL, PATH, MAILTO, HOME, CRON_TZ.")]
    [Nullable(false)]
    public Dictionary<string, string>? Variables { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Whether the environment settings exist. When false, removes all environment variables from the crontab.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
