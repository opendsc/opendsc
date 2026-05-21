// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Contracts.Permissions;

/// <summary>
/// Type of principal for ACL entries.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<PrincipalType>))]
public enum PrincipalType
{
    /// <summary>
    /// The principal is a user account.
    /// </summary>
    User = 0,

    /// <summary>
    /// The principal is a group.
    /// </summary>
    Group = 1
}

/// <summary>
/// Permission level for resource access.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ResourcePermission>))]
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
