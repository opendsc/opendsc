// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

/// <summary>
/// Personal Access Token for API authentication.
/// </summary>
public class PersonalAccessToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public string TokenPrefix { get; set; } = string.Empty;

    public string Scopes { get; set; } = string.Empty;

    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public string? LastUsedIpAddress { get; set; }

    public bool IsRevoked { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}

/// <summary>
/// External login provider linkage.
/// </summary>
public class ExternalLogin
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ProviderKey { get; set; } = string.Empty;

    public string? ProviderDisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
