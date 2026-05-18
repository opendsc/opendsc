// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

/// <summary>
/// Request to create a new user account.
/// </summary>
public sealed class CreateUserRequest
{
    /// <summary>
    /// The username for the new account.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The email address for the new account.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// The initial password for the new account.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// The type of account (User, Service, etc.).
    /// </summary>
    public AccountType AccountType { get; set; }

    /// <summary>
    /// Whether the user must change their password on first login.
    /// </summary>
    public bool RequirePasswordChange { get; set; }

    /// <summary>
    /// Optional description or notes about the user.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request to update an existing user account.
/// </summary>
public sealed class UpdateUserRequest
{
    /// <summary>
    /// The updated username.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The updated email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Whether the account is active and can authenticate.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether the user must change their password on next login.
    /// </summary>
    public bool RequirePasswordChange { get; set; }

    /// <summary>
    /// The account type (User, Service, etc.).
    /// </summary>
    public AccountType AccountType { get; set; }

    /// <summary>
    /// Optional description or notes about the user.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the account is locked and cannot authenticate.
    /// </summary>
    public bool IsLocked { get; set; }
}

/// <summary>
/// Request to reset a user's password (admin operation, no current password verification required).
/// </summary>
public sealed class ResetPasswordRequest
{
    /// <summary>
    /// The new password to set for the user.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request to change a user's password (requires current password verification).
/// </summary>
public sealed class ChangePasswordRequest
{
    /// <summary>
    /// The user's current password for verification.
    /// </summary>
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>
    /// The new password to set.
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// Request to assign a single role to a user.
/// </summary>
public sealed class AssignRoleRequest
{
    /// <summary>
    /// The ID of the role to assign to the user.
    /// </summary>
    public Guid RoleId { get; set; }
}

/// <summary>
/// Request to remove a single role from a user.
/// </summary>
public sealed class RemoveRoleRequest
{
    /// <summary>
    /// The ID of the role to remove from the user.
    /// </summary>
    public Guid RoleId { get; set; }
}

/// <summary>
/// Request to set all roles for a user (replaces existing role assignments).
/// </summary>
public sealed class SetUserRolesRequest
{
    /// <summary>
    /// The complete list of role IDs to assign to the user. Replaces all existing role assignments.
    /// </summary>
    public IReadOnlyList<Guid> RoleIds { get; set; } = [];
}

/// <summary>
/// Request to create a new role.
/// </summary>
public sealed class CreateRoleRequest
{
    /// <summary>
    /// The name of the role.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the role's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The list of permission identifiers assigned to this role.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; set; } = [];
}

/// <summary>
/// Request to update an existing role.
/// </summary>
public sealed class UpdateRoleRequest
{
    /// <summary>
    /// The updated role name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The updated description of the role's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The updated list of permission identifiers for this role.
    /// </summary>
    public IReadOnlyList<string> Permissions { get; set; } = [];
}

/// <summary>
/// Request to set all groups associated with a role (replaces existing group assignments).
/// </summary>
public sealed class SetRoleGroupsRequest
{
    /// <summary>
    /// The complete list of group IDs to associate with the role. Replaces all existing group assignments.
    /// </summary>
    public IReadOnlyList<Guid> GroupIds { get; set; } = [];
}

/// <summary>
/// Request to create a new group.
/// </summary>
public sealed class CreateGroupRequest
{
    /// <summary>
    /// The name of the group.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the group's purpose.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request to update an existing group.
/// </summary>
public sealed class UpdateGroupRequest
{
    /// <summary>
    /// The updated group name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The updated description of the group's purpose.
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// Request to add a user to a group.
/// </summary>
public sealed class AddGroupMemberRequest
{
    /// <summary>
    /// The ID of the user to add to the group.
    /// </summary>
    public Guid UserId { get; set; }
}

/// <summary>
/// Request to remove a user from a group.
/// </summary>
public sealed class RemoveGroupMemberRequest
{
    /// <summary>
    /// The ID of the user to remove from the group.
    /// </summary>
    public Guid UserId { get; set; }
}

/// <summary>
/// Request to assign a single role to a group.
/// </summary>
public sealed class AssignGroupRoleRequest
{
    /// <summary>
    /// The ID of the role to assign to the group.
    /// </summary>
    public Guid RoleId { get; set; }
}

/// <summary>
/// Request to remove a single role from a group.
/// </summary>
public sealed class RemoveGroupRoleRequest
{
    /// <summary>
    /// The ID of the role to remove from the group.
    /// </summary>
    public Guid RoleId { get; set; }
}

/// <summary>
/// Request to set all members of a group (replaces existing membership).
/// </summary>
public sealed class SetGroupMembersRequest
{
    /// <summary>
    /// The complete list of user IDs for the group's membership. Replaces all existing members.
    /// </summary>
    public IReadOnlyList<Guid> UserIds { get; set; } = [];
}

/// <summary>
/// Request to set all roles associated with a group (replaces existing role assignments).
/// </summary>
public sealed class SetGroupRolesRequest
{
    /// <summary>
    /// The complete list of role IDs to assign to the group. Replaces all existing role assignments.
    /// </summary>
    public IReadOnlyList<Guid> RoleIds { get; set; } = [];
}

/// <summary>
/// Request to create a mapping between an external identity provider group and a local group.
/// </summary>
public sealed class CreateExternalGroupMappingRequest
{
    /// <summary>
    /// The name of the external identity provider (e.g., "OIDC", "SAML", "ActiveDirectory").
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the external group in the identity provider.
    /// </summary>
    public string ExternalGroupId { get; set; } = string.Empty;

    /// <summary>
    /// Optional display name of the external group.
    /// </summary>
    public string? ExternalGroupName { get; set; }

    /// <summary>
    /// The ID of the local group to map to the external group.
    /// </summary>
    public Guid GroupId { get; set; }
}

/// <summary>
/// Request to update the scopes/permissions for a token.
/// </summary>
public sealed class UpdateTokenScopesRequest
{
    /// <summary>
    /// The updated list of scopes/permissions for the token (e.g., "read:configurations", "write:nodes").
    /// </summary>
    public IReadOnlyList<string> Scopes { get; set; } = [];
}

/// <summary>
/// Request to authenticate a user and obtain credentials.
/// </summary>
public sealed class LoginRequest
{
    /// <summary>
    /// The username to authenticate.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The password for the user account.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request to create a new API token for programmatic access.
/// </summary>
public sealed class CreateTokenRequest
{
    /// <summary>
    /// A user-friendly name for the token (e.g., "CI/CD Pipeline Token").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The list of scopes/permissions for the token (e.g., "read:configurations", "write:nodes").
    /// </summary>
    public IReadOnlyList<string> Scopes { get; set; } = [];

    /// <summary>
    /// Optional expiration date/time for the token. If null, the token does not expire.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; set; }
}
