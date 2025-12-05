// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace TestResource.NonAot;

public sealed class Schema
{
    [JsonRequired]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("_exist")]
    public bool? Exist { get; set; }

    [JsonPropertyName("_inDesiredState")]
    public bool? InDesiredState { get; set; }
}
