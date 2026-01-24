// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer.ServerRole;

[Title("SQL Server Server Role Schema")]
[Description("Schema for managing SQL Server server roles via OpenDsc.")]
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
    [Description("The name of the server role.")]
    [Pattern(@"^.+$")]
    public string Name { get; set; } = string.Empty;

    [Description("The owner of the server role. Can be a login or another server role.")]
    [Nullable(false)]
    public string? Owner { get; set; }

    [Description("The members of the server role. Members can be server logins or other server roles.")]
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
    public DateTime? DateCreated { get; set; }

    [ReadOnly]
    [Description("The date the role was last modified.")]
    public DateTime? DateModified { get; set; }

    [ReadOnly]
    [Description("Whether this is a fixed server role (e.g., sysadmin, serveradmin, securityadmin).")]
    public bool? IsFixedRole { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the server role exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
