// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

using PermissionState = Microsoft.SqlServer.Management.Smo.PermissionState;

namespace OpenDsc.Resource.SqlServer.ObjectPermission;

[Title("SQL Server Object Permission Schema")]
[Description("Schema for managing SQL Server object permissions (tables, views, stored procedures, etc.) via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
    internal const string DefaultState = "Grant";

    [Required]
    [Description("The name of the SQL Server instance to connect to. Use '.' or '(local)' for the default local instance, or 'servername\\instancename' for named instances.")]
    [Pattern(@"^.+$")]
    public string ServerInstance { get; set; } = string.Empty;

    [Description("The username for SQL Server authentication when connecting to the server. If not specified, Windows Authentication is used.")]
    [Nullable(false)]
    [WriteOnly]
    public string? ConnectUsername { get; set; }

    [Description("The password for SQL Server authentication when connecting to the server. Required when ConnectUsername is specified.")]
    [Nullable(false)]
    [WriteOnly]
    public string? ConnectPassword { get; set; }

    [Required]
    [Description("The name of the database containing the object.")]
    [Pattern(@"^[^\[\]]+$")]
    public string DatabaseName { get; set; } = string.Empty;

    [Description("The schema of the object (e.g., 'dbo'). Defaults to 'dbo' if not specified.")]
    [Pattern(@"^[^\[\]]+$")]
    [Nullable(false)]
    public string? SchemaName { get; set; }

    [Required]
    [Description("The type of the database object (Table, View, StoredProcedure, UserDefinedFunction, Schema, Sequence, Synonym).")]
    public ObjectType ObjectType { get; set; }

    [Required]
    [Description("The name of the database object to manage permissions on.")]
    [Pattern(@"^[^\[\]]+$")]
    public string ObjectName { get; set; } = string.Empty;

    [Required]
    [Description("The name of the principal (user or role) to grant or deny permissions to.")]
    [Pattern(@"^.+$")]
    public string Principal { get; set; } = string.Empty;

    [Required]
    [Description("The permission to grant or deny (e.g., 'Select', 'Insert', 'Update', 'Delete', 'Execute', 'References', 'ViewDefinition', 'Alter', 'Control', 'TakeOwnership').")]
    [Pattern(@"^[A-Za-z]+$")]
    public string Permission { get; set; } = string.Empty;

    [Description("The state of the permission (Grant, GrantWithGrant, or Deny).")]
    [Nullable(false)]
    [Default(DefaultState)]
    public PermissionState? State { get; set; }

    [Description("The grantor of the permission.")]
    [ReadOnly]
    [Nullable(false)]
    public string? Grantor { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the permission exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}

/// <summary>
/// Supported database object types for permission management.
/// </summary>
public enum ObjectType
{
    /// <summary>Table object.</summary>
    Table,

    /// <summary>View object.</summary>
    View,

    /// <summary>Stored procedure object.</summary>
    StoredProcedure,

    /// <summary>User-defined function object.</summary>
    UserDefinedFunction,

    /// <summary>Schema object.</summary>
    Schema,

    /// <summary>Sequence object.</summary>
    Sequence,

    /// <summary>Synonym object.</summary>
    Synonym
}
