// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

/// <summary>
/// Represents a role with associated permissions.
/// </summary>
public class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }

    public string Permissions { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }
}

/// <summary>
/// Represents an internal group for organizing users.
/// </summary>
public class Group
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystemGroup { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }
}

/// <summary>
/// Maps external groups (AD, OAuth) to internal groups.
/// </summary>
public class ExternalGroupMapping
{
    public Guid Id { get; set; }

    public string ExternalGroupId { get; set; } = string.Empty;

    public string? ExternalGroupName { get; set; }

    public string Provider { get; set; } = string.Empty;

    public Guid GroupId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }
}

/// <summary>
/// Many-to-many relationship between users and roles.
/// </summary>
public class UserRole
{
    public Guid UserId { get; set; }

    public Guid RoleId { get; set; }
}

/// <summary>
/// Many-to-many relationship between users and groups.
/// </summary>
public class UserGroup
{
    public Guid UserId { get; set; }

    public Guid GroupId { get; set; }
}

/// <summary>
/// Many-to-many relationship between groups and roles.
/// </summary>
public class GroupRole
{
    public Guid GroupId { get; set; }

    public Guid RoleId { get; set; }
}
