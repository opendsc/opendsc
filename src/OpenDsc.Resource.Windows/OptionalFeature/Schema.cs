// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.OptionalFeature;

[Title("Windows Optional Feature Schema")]
[Description("Schema for managing Windows optional features via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Pattern(@"^[a-zA-Z0-9\-\.]+$")]
    [Description("The name of the Windows feature to manage.")]
    public required string Name { get; set; } = string.Empty;

    [Description("Indicates whether to include all sub-features when enabling or disabling the feature.")]
    [Nullable(false)]
    public bool? IncludeAllSubFeatures { get; set; }

    [Description("Specifies the location of source files for the feature. If not specified, Windows will use its default source location.")]
    [Nullable(false)]
    public string[]? Source { get; set; }

    [Description("The current state of the feature.")]
    [ReadOnly]
    [Nullable(false)]
    public DismPackageFeatureState? State { get; set; }

    [Description("The display name of the feature.")]
    [ReadOnly]
    [Nullable(false)]
    public string? DisplayName { get; set; }

    [Description("The description of the feature.")]
    [ReadOnly]
    [Nullable(false)]
    public string? Description { get; set; }

    [JsonPropertyName("_exist")]
    [Default(true)]
    [Description("Indicates whether the feature should exist (true) or not exist (false).")]
    [Nullable(false)]
    public bool? Exist { get; set; }

    [JsonPropertyName("_metadata")]
    [Description("Metadata about the operation, including restart requirements.")]
    [ReadOnly]
    [Nullable(false)]
    public ResourceMetadata? Metadata { get; set; }
}
