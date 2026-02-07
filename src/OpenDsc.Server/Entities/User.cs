// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

/// <summary>
/// Represents a user or service account.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? PasswordHash { get; set; }

    public string? PasswordSalt { get; set; }

    public AccountType AccountType { get; set; } = AccountType.User;

    public bool EmailConfirmed { get; set; }

    public bool RequirePasswordChange { get; set; }

    public DateTimeOffset? LockoutEnd { get; set; }

    public int AccessFailedCount { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Description { get; set; }

    public Guid? ManagedByGroupId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }
}
