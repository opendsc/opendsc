// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Contracts.Permissions;

/// <summary>
/// Request to grant or update a permission on a resource.
/// </summary>
public sealed class GrantPermissionRequest
{
    /// <summary>
    /// The type of principal. Must be "User" or "Group".
    /// </summary>
    [JsonRequired]
    public string PrincipalType { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the user or group.
    /// </summary>
    [JsonRequired]
    public Guid PrincipalId { get; set; }

    /// <summary>
    /// The permission level to grant. Must be "Read", "Modify", or "Manage".
    /// </summary>
    [JsonRequired]
    public string Level { get; set; } = string.Empty;
}

/// <summary>
/// A single ACL entry on a resource.
/// </summary>
public sealed class PermissionEntry
{
    /// <summary>
    /// The type of principal ("User" or "Group").
    /// </summary>
    public string PrincipalType { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the user or group.
    /// </summary>
    public Guid PrincipalId { get; set; }

    /// <summary>
    /// The display name of the principal (username or group name).
    /// </summary>
    public string PrincipalName { get; set; } = string.Empty;

    /// <summary>
    /// The permission level ("Read", "Modify", or "Manage").
    /// </summary>
    public string Level { get; set; } = string.Empty;

    /// <summary>
    /// When this permission was granted.
    /// </summary>
    public DateTimeOffset GrantedAt { get; set; }

    /// <summary>
    /// The ID of the user who granted this permission, if available.
    /// </summary>
    public Guid? GrantedByUserId { get; set; }
}

/// <summary>
/// Request to revoke a permission from a principal on a resource.
/// </summary>
public sealed class RevokePermissionRequest
{
    /// <summary>
    /// The unique identifier of the user or group.
    /// </summary>
    [JsonRequired]
    public Guid PrincipalId { get; set; }

    /// <summary>
    /// The type of principal. Must be "User" or "Group".
    /// </summary>
    [JsonRequired]
    public string PrincipalType { get; set; } = string.Empty;
}
