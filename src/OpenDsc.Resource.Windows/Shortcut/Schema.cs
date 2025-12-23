// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.Shortcut;

[Title("Windows Shortcut Resource Schema")]
[Description("Schema for managing Windows shortcuts via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    internal const string DefaultIconLocation = ",0";
    internal const string DefaultWindowStyle = "Normal";

    [Required]
    [Description("The full path to the shortcut file.")]
    public string Path { get; set; } = string.Empty;

    [Description("The target full path the shortcut points to.")]
    [Nullable(false)]
    public string? TargetPath { get; set; }

    [Description("The arguments passed to the target when the shortcut is executed.")]
    [Nullable(false)]
    public string? Arguments { get; set; }

    [Description("The working directory for the shortcut target.")]
    [Nullable(false)]
    public string? WorkingDirectory { get; set; }

    [Description("The description of the shortcut.")]
    [Nullable(false)]
    public string? Description { get; set; }

    [Description("The location of the icon for the shortcut.")]
    [Default(DefaultIconLocation)]
    [Nullable(false)]
    public string? IconLocation { get; set; }

    [Description("The hotkey assigned to the shortcut.")]
    [Nullable(false)]
    public string? Hotkey { get; set; }

    [Description("The window style when the shortcut target is executed.")]
    [Nullable(false)]
    [Default(DefaultWindowStyle)]
    public WindowStyle? WindowStyle { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the shortcut exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
