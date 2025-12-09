// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Resource;

/// <summary>
/// Represents an informational log message for DSC resources.
/// </summary>
public sealed class Info
{
    /// <summary>
    /// Gets or sets the informational message text.
    /// </summary>
    [JsonPropertyName("info")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Represents a warning log message for DSC resources.
/// </summary>
public sealed class Warning
{
    /// <summary>
    /// Gets or sets the warning message text.
    /// </summary>
    [JsonPropertyName("warn")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Represents an error log message for DSC resources.
/// </summary>
public sealed class Error
{
    /// <summary>
    /// Gets or sets the error message text.
    /// </summary>
    [JsonPropertyName("error")]
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Represents a trace/debug log message for DSC resources.
/// </summary>
public sealed class Trace
{
    /// <summary>
    /// Gets or sets the trace message text.
    /// </summary>
    [JsonPropertyName("trace")]
    public string Message { get; set; } = string.Empty;
}
