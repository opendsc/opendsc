// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

/// <summary>
/// Type of user account.
/// </summary>
public enum AccountType
{
    /// <summary>
    /// Regular human user account.
    /// </summary>
    User,

    /// <summary>
    /// Service account for automation (PAT-only authentication).
    /// </summary>
    ServiceAccount
}

/// <summary>
/// Type of principal for ACL entries.
/// </summary>
public enum PrincipalType
{
    /// <summary>
    /// Individual user.
    /// </summary>
    User,

    /// <summary>
    /// Group of users.
    /// </summary>
    Group
}

/// <summary>
/// Permission level for resource access.
/// </summary>
public enum ResourcePermission
{
    /// <summary>
    /// Read-only access to resource.
    /// </summary>
    Read = 0,

    /// <summary>
    /// Read and write access to resource content.
    /// </summary>
    Modify = 1,

    /// <summary>
    /// Full control including delete and ACL management.
    /// </summary>
    Manage = 2
}

/// <summary>
/// Controls how scope values are handled for a scope type.
/// </summary>
public enum ScopeValueMode
{
    /// <summary>
    /// Any value can be entered (free-form text input).
    /// </summary>
    Unrestricted = 0,

    /// <summary>
    /// Only predefined scope values are allowed (must select from list).
    /// </summary>
    Restricted = 1
}
