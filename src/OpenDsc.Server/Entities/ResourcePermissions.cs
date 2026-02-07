// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Entities;

/// <summary>
/// Resource-level permissions for configurations.
/// </summary>
public class ConfigurationPermission
{
    public Guid Id { get; set; }

    public Guid ConfigurationId { get; set; }

    public PrincipalType PrincipalType { get; set; }

    public Guid PrincipalId { get; set; }

    public ResourcePermission PermissionLevel { get; set; }

    public DateTimeOffset GrantedAt { get; set; }

    public Guid? GrantedByUserId { get; set; }
}

/// <summary>
/// Resource-level permissions for composite configurations.
/// </summary>
public class CompositeConfigurationPermission
{
    public Guid Id { get; set; }

    public Guid CompositeConfigurationId { get; set; }

    public PrincipalType PrincipalType { get; set; }

    public Guid PrincipalId { get; set; }

    public ResourcePermission PermissionLevel { get; set; }

    public DateTimeOffset GrantedAt { get; set; }

    public Guid? GrantedByUserId { get; set; }
}

/// <summary>
/// Resource-level permissions for parameters.
/// </summary>
public class ParameterPermission
{
    public Guid Id { get; set; }

    public Guid ParameterId { get; set; }

    public PrincipalType PrincipalType { get; set; }

    public Guid PrincipalId { get; set; }

    public ResourcePermission PermissionLevel { get; set; }

    public DateTimeOffset GrantedAt { get; set; }

    public Guid? GrantedByUserId { get; set; }
}
