// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

/// <summary>
/// Summary information for a user account.
/// </summary>
public sealed class UserSummary
{
    /// <summary>
    /// The unique identifier for the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The username for the account.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The email address associated with the user account.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The type of account (User, Service, etc.).
    /// </summary>
    public AccountType AccountType { get; set; }

    /// <summary>
    /// Whether the account is active and can authenticate.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the user must change their password on next login.
    /// </summary>
    public bool RequirePasswordChange { get; set; }

    /// <summary>
    /// The date/time when the account is locked out, if applicable. Null if not locked out.
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    /// The timestamp when the user account was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The timestamp when the user account was last modified, if applicable.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }
}

/// <summary>
/// Summary information for a role.
/// </summary>
public sealed class RoleSummary
{
    /// <summary>
    /// The unique identifier for the role.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the role.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the role's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a system-defined role that cannot be deleted.
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// The list of permission identifiers granted by this role.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; set; } = [];

    /// <summary>
    /// The timestamp when the role was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The timestamp when the role was last modified, if applicable.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }
}

/// <summary>
/// Summary information for a group.
/// </summary>
public sealed class GroupSummary
{
    /// <summary>
    /// The unique identifier for the group.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the group.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the group's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a system-defined group that cannot be deleted.
    /// </summary>
    public bool IsSystemGroup { get; set; }

    /// <summary>
    /// The timestamp when the group was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The timestamp when the group was last modified, if applicable.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }
}

/// <summary>
/// Detailed information for a user account, including related roles and groups.
/// </summary>
public sealed class UserDetails
{
    /// <summary>
    /// The unique identifier for the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The username for the account.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The email address associated with the user account.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The type of account (User, Service, etc.).
    /// </summary>
    public AccountType AccountType { get; set; }

    /// <summary>
    /// Whether the account is active and can authenticate.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the user must change their password on next login.
    /// </summary>
    public bool RequirePasswordChange { get; set; }

    /// <summary>
    /// The date/time when the account is locked out, if applicable. Null if not locked out.
    /// </summary>
    public DateTimeOffset? LockoutEnd { get; set; }

    /// <summary>
    /// The number of failed authentication attempts against this account.
    /// </summary>
    public int AccessFailedCount { get; set; }

    /// <summary>
    /// Optional description or notes about the user.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The timestamp when the user account was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The timestamp when the user account was last modified, if applicable.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// The list of roles assigned to the user.
    /// </summary>
    public IReadOnlyList<RoleSummary> Roles { get; set; } = [];

    /// <summary>
    /// The list of groups the user belongs to.
    /// </summary>
    public IReadOnlyList<GroupSummary> Groups { get; set; } = [];
}

/// <summary>
/// Detailed information for a role, including its permissions.
/// </summary>
public sealed class RoleDetails
{
    /// <summary>
    /// The unique identifier for the role.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the role.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the role's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The list of permission identifiers granted by this role.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; set; } = [];

    /// <summary>
    /// Whether this is a system-defined role that cannot be deleted.
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// The timestamp when the role was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The timestamp when the role was last modified, if applicable.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }
}

/// <summary>
/// Detailed information for a group, including members and assigned roles.
/// </summary>
public sealed class GroupDetails
{
    /// <summary>
    /// The unique identifier for the group.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the group.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The description of the group's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a system-defined group that cannot be deleted.
    /// </summary>
    public bool IsSystemGroup { get; set; }

    /// <summary>
    /// The timestamp when the group was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The timestamp when the group was last modified, if applicable.
    /// </summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// The list of users who are members of the group.
    /// </summary>
    public IReadOnlyList<UserSummary> Members { get; set; } = [];

    /// <summary>
    /// The list of roles assigned to the group.
    /// </summary>
    public IReadOnlyList<RoleSummary> Roles { get; set; } = [];
}

/// <summary>
/// Information about an external identity provider group mapping.
/// </summary>
public sealed class ExternalGroupMappingInfo
{
    /// <summary>
    /// The unique identifier for the group mapping.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The name of the external identity provider (e.g., "OIDC", "SAML", "ActiveDirectory").
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the group in the external identity provider.
    /// </summary>
    public string ExternalGroupId { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the external group.
    /// </summary>
    public string? ExternalGroupName { get; set; }

    /// <summary>
    /// The unique identifier of the local group mapped to the external group.
    /// </summary>
    public Guid GroupId { get; set; }

    /// <summary>
    /// The name of the local group that is mapped to the external group.
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the group mapping was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>
/// Details about the currently authenticated user.
/// </summary>
public sealed class CurrentUserDetails
{
    /// <summary>
    /// The unique identifier for the authenticated user.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// The username of the authenticated user.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The email address of the authenticated user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The type of account (User, Service, etc.).
    /// </summary>
    public AccountType AccountType { get; set; }

    /// <summary>
    /// The list of role names assigned to the authenticated user.
    /// </summary>
    public IReadOnlyList<string> Roles { get; set; } = [];

    /// <summary>
    /// The authentication provider used to authenticate the user (e.g., "local", "oidc").
    /// </summary>
    public string? AuthProvider { get; set; }
}

/// <summary>
/// Result of an authentication attempt.
/// </summary>
public sealed class AuthenticationResult
{
    /// <summary>
    /// Whether the authentication attempt was successful.
    /// </summary>
    public bool IsAuthenticated { get; set; }

    /// <summary>
    /// Whether the user account is currently locked out.
    /// </summary>
    public bool IsLockedOut { get; set; }

    /// <summary>
    /// Summary information about the authenticated user, if authentication was successful. Null if authentication failed.
    /// </summary>
    public UserSummary? User { get; set; }
}
