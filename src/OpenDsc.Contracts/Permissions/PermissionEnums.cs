// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Permissions;

/// <summary>
/// Type of principal for ACL entries.
/// </summary>
public enum PrincipalType
{
    /// <summary>
    /// The principal is a user account.
    /// </summary>
    User,

    /// <summary>
    /// The principal is a group.
    /// </summary>
    Group
}

/// <summary>
/// Permission level for resource access.
/// </summary>
public enum ResourcePermission
{
    /// <summary>
    /// Read-only access to the resource.
    /// </summary>
    Read = 0,

    /// <summary>
    /// Read and modify access to the resource.
    /// </summary>
    Modify = 1,

    /// <summary>
    /// Full access to manage the resource, including permissions.
    /// </summary>
    Manage = 2
}
