// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

using OpenDsc.Schema;

namespace OpenDsc.Resource.Windows.Environment;

[Title("Windows Environment Variable Schema")]
[Description("Schema for managing Windows environment variables via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Description("The name of the environment variable.")]
    [Pattern(@"^[^\x00=][^=]{0,253}$")]
    public string Name { get; set; } = string.Empty;

    [Description("The environment variable value.")]
    [Nullable(false)]
    public string? Value { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the environment variable exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }

    [JsonPropertyName("_scope")]
    [Description("If environment variable should be user or machine scope.")]
    [Nullable(false)]
    [Default("User")]
    public DscScope? Scope { get; set; }
}
