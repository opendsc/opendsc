// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Schema;

/// <summary>
/// A trace message from DSC stderr output.
/// </summary>
public sealed class DscTraceMessage
{
    /// <summary>
    /// The timestamp of the message.
    /// </summary>
    public string? Timestamp { get; set; }

    /// <summary>
    /// The message level (error, warn, info, debug, trace).
    /// </summary>
    public DscTraceLevel? Level { get; set; }

    /// <summary>
    /// The message fields.
    /// </summary>
    public DscTraceFields? Fields { get; set; }
}

/// <summary>
/// The fields of a DSC trace message.
/// </summary>
public sealed class DscTraceFields
{
    /// <summary>
    /// The message content.
    /// </summary>
    public string? Message { get; set; }
}
