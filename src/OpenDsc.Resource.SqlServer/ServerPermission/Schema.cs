// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer.ServerPermission;

[Title("SQL Server Server Permission Schema")]
[Description("Schema for managing SQL Server server-level permissions via OpenDsc.")]
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
    [Description("The name of the principal (login or server role) to grant or deny permissions to.")]
    [Pattern(@"^.+$")]
    public string Principal { get; set; } = string.Empty;

    [Required]
    [Description("The server-level permission to grant or deny.")]
    [Nullable(false)]
    public ServerPermissionName Permission { get; set; }

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

    [JsonPropertyName("_inDesiredState")]
    [Description("Indicates whether the permission is in the desired state.")]
    [ReadOnly]
    [Nullable(false)]
    public bool? InDesiredState { get; set; }
}
