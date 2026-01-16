// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.ScheduledTask;

[Title("Scheduled Task Action")]
[Description("Configuration for executing an application or script.")]
[AdditionalProperties(false)]
public sealed class Action
{
    [Required]
    [Description("The path to the executable or script to run.")]
    public string Path { get; set; } = string.Empty;

    [Description("The arguments to pass to the executable.")]
    [Nullable(false)]
    public string? Arguments { get; set; }

    [Description("The working directory for the executable.")]
    [Nullable(false)]
    public string? WorkingDirectory { get; set; }
}
