// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

/// <summary>
/// Tracks metadata about resource discovery operations (single record).
/// </summary>
public sealed class DiscoveryMetadata
{
    /// <summary>
    /// Always 1 - this is a singleton entity.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Timestamp of the last successful discovery operation (UTC).
    /// </summary>
    public DateTimeOffset? LastDiscoveredAt { get; set; }
}
