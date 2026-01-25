// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

/// <summary>
/// Represents a DSC configuration document stored on the server.
/// </summary>
public sealed class Configuration
{
    /// <summary>
    /// Unique identifier for the configuration.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable name of the configuration.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The DSC configuration document content (YAML or JSON).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// SHA256 checksum of the content for change detection.
    /// </summary>
    public string Checksum { get; set; } = string.Empty;

    /// <summary>
    /// When the configuration was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the configuration was last modified.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }
}
