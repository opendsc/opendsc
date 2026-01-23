// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer.DatabaseRole;

[Title("SQL Server Database Role Schema")]
[Description("Schema for managing SQL Server database roles via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
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
    [Description("The name of the database containing the role.")]
    [Pattern(@"^[^\[\]]+$")]
    public string DatabaseName { get; set; } = string.Empty;

    [Required]
    [Description("The name of the database role.")]
    [Pattern(@"^.+$")]
    public string Name { get; set; } = string.Empty;

    [Description("The owner of the database role. Can be a user or another role.")]
    [Nullable(false)]
    public string? Owner { get; set; }

    [Description("The members of the database role. Members can be database users or other database roles.")]
    [Nullable(false)]
    [UniqueItems(true)]
    public string[]? Members { get; set; }

    [JsonPropertyName("_purge")]
    [Description("When true, removes members not in the Members list. When false, only adds members from the Members list without removing others. Only applicable when Members is specified.")]
    [Nullable(false)]
    [WriteOnly]
    [Default(false)]
    public bool? Purge { get; set; }

    [ReadOnly]
    [Description("The creation date of the role.")]
    public DateTime? CreateDate { get; set; }

    [ReadOnly]
    [Description("The date the role was last modified.")]
    public DateTime? DateLastModified { get; set; }

    [ReadOnly]
    [Description("Whether this is a fixed database role (e.g., db_owner, db_datareader).")]
    public bool? IsFixedRole { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the database role exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
