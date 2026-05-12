// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Users;

public sealed class CreateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public AccountType AccountType { get; set; } = AccountType.User;
    public bool RequirePasswordChange { get; set; } = true;
    public string? Description { get; set; }
}

public sealed class UpdateUserRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool RequirePasswordChange { get; set; }
    public AccountType AccountType { get; set; } = AccountType.User;
    public string? Description { get; set; }
    public bool IsLocked { get; set; }
}

public sealed class ResetPasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class AssignRoleRequest
{
    public Guid RoleId { get; set; }
}

public sealed class RemoveRoleRequest
{
    public Guid RoleId { get; set; }
}

public sealed class SetUserRolesRequest
{
    public Guid[] RoleIds { get; set; } = [];
}

public sealed class CreateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string[] Permissions { get; set; } = [];
}

public sealed class UpdateRoleRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string[] Permissions { get; set; } = [];
}

public sealed class SetRoleGroupsRequest
{
    public Guid[] GroupIds { get; set; } = [];
}

public sealed class CreateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class UpdateGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class AddGroupMemberRequest
{
    public Guid UserId { get; set; }
}

public sealed class RemoveGroupMemberRequest
{
    public Guid UserId { get; set; }
}

public sealed class AssignGroupRoleRequest
{
    public Guid RoleId { get; set; }
}

public sealed class RemoveGroupRoleRequest
{
    public Guid RoleId { get; set; }
}

public sealed class SetGroupMembersRequest
{
    public Guid[] UserIds { get; set; } = [];
}

public sealed class SetGroupRolesRequest
{
    public Guid[] RoleIds { get; set; } = [];
}

public sealed class CreateExternalGroupMappingRequest
{
    public string Provider { get; set; } = string.Empty;
    public string ExternalGroupId { get; set; } = string.Empty;
    public string? ExternalGroupName { get; set; }
    public Guid GroupId { get; set; }
}

public sealed class UpdateTokenScopesRequest
{
    public string[] Scopes { get; set; } = [];
}
