// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Resource;

/// <summary>
/// Represents the result of a Delete operation invoked in what-if mode.
/// </summary>
public sealed class DeleteResult
{
    /// <summary>
    /// Gets or sets the metadata describing the changes that deletion would apply.
    /// </summary>
    [JsonPropertyName("_metadata")]
    public DeleteWhatIfMetadata? Metadata { get; set; }
}

/// <summary>
/// Represents the what-if metadata returned by a Delete operation in what-if mode.
/// </summary>
public sealed class DeleteWhatIfMetadata
{
    /// <summary>
    /// Gets or sets the messages describing the changes that deletion would apply.
    /// </summary>
    [JsonPropertyName("whatIf")]
    public string[]? WhatIf { get; set; }
}
