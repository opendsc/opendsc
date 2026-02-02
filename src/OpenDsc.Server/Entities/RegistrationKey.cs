// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

/// <summary>
/// Represents a registration key for node authorization.
/// </summary>
public sealed class RegistrationKey
{
    /// <summary>
    /// Unique identifier for the registration key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The registration key value (Base64-encoded).
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// When the key expires and can no longer be used.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>
    /// When the key was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Maximum number of times this key can be used (null = unlimited).
    /// </summary>
    public int? MaxUses { get; set; }

    /// <summary>
    /// Current number of times this key has been used.
    /// </summary>
    public int CurrentUses { get; set; }

    /// <summary>
    /// Whether the key has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }
}
