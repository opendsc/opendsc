// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

/// <summary>
/// Record of a retention cleanup run.
/// </summary>
public sealed class RetentionRun
{
    public Guid Id { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// The type of versions cleaned up: "Configuration", "Parameter", or "CompositeConfiguration".
    /// </summary>
    public required string VersionType { get; set; }

    /// <summary>
    /// True when triggered by the background scheduler; false when triggered manually via the API.
    /// </summary>
    public bool IsScheduled { get; set; }

    public bool IsDryRun { get; set; }
    public int DeletedCount { get; set; }
    public int KeptCount { get; set; }
    public string? Error { get; set; }
}
