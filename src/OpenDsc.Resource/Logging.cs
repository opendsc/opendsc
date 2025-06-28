// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Resource;

public sealed class Info
{
    [JsonPropertyName("info")]
    public string Message { get; set; } = string.Empty;
}

public sealed class Warning
{
    [JsonPropertyName("warn")]
    public string Message { get; set; } = string.Empty;
}

public sealed class Error
{
    [JsonPropertyName("error")]
    public string Message { get; set; } = string.Empty;
}

public sealed class Trace
{
    [JsonPropertyName("trace")]
    public string Message { get; set; } = string.Empty;
}
