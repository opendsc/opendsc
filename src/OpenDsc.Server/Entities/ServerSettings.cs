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
    /// Default interval for certificate rotation (informational).
    /// </summary>
    public TimeSpan CertificateRotationInterval { get; set; } = TimeSpan.FromDays(60);
}
