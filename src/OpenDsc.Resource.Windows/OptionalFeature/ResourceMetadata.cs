// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.OptionalFeature;

public sealed class ResourceMetadata
{
    [JsonPropertyName("_restartRequired")]
    [Nullable(false)]
    [ReadOnly]
    public RestartRequired[]? RestartRequired { get; set; }
}
