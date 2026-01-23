// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

/// <summary>
/// Server-wide settings stored in the database.
/// </summary>
public sealed class ServerSettings
{
    /// <summary>
    /// Unique identifier (singleton row).
    /// </summary>
    public int Id { get; set; } = 1;

    /// <summary>
    /// Registration key for new nodes to register.
    /// </summary>
    public string RegistrationKey { get; set; } = string.Empty;

    /// <summary>
    /// Admin API key for management operations.
    /// </summary>
    public string AdminApiKeyHash { get; set; } = string.Empty;

    /// <summary>
    /// How often nodes should rotate their API keys.
    /// </summary>
    public TimeSpan KeyRotationInterval { get; set; } = TimeSpan.FromDays(7);
}
