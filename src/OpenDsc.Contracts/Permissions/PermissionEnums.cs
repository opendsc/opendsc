// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Permissions;

/// <summary>
/// Type of principal for ACL entries.
/// </summary>
public enum PrincipalType
{
    User,
    Group
}

/// <summary>
/// Permission level for resource access.
/// </summary>
public enum ResourcePermission
{
    Read = 0,
    Modify = 1,
    Manage = 2
}
