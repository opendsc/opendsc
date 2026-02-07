// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

/// <summary>
/// Audit log entry for tracking administrative actions and permission changes.
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    public Guid? UserId { get; set; }

    public string? Username { get; set; }

    public string Action { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public Guid? ResourceId { get; set; }

    public string? Details { get; set; }

    public string? IpAddress { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}
