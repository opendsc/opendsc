// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

using LoginType = Microsoft.SqlServer.Management.Smo.LoginType;

namespace OpenDsc.Resource.SqlServer.Login;

[Title("SQL Server Login Schema")]
[Description("Schema for managing SQL Server logins via OpenDsc.")]
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
    [Description("The name of the login.")]
    [Pattern(@"^.+$")]
    public string Name { get; set; } = string.Empty;

    [Description("The type of login.")]
    [Nullable(false)]
    public LoginType? LoginType { get; set; }

    [Description("The password for the login. Write-only, required when creating SQL logins. Not applicable for Windows authentication.")]
    [Nullable(false)]
    [WriteOnly]
    public string? Password { get; set; }

    [Description("The default database for the login.")]
    [Nullable(false)]
    public string? DefaultDatabase { get; set; }

    [Description("The default language for the login.")]
    [Nullable(false)]
    public string? Language { get; set; }

    [Description("Whether the login is disabled.")]
    [Nullable(false)]
    public bool? Disabled { get; set; }

    [Description("Whether password expiration policy is enforced.")]
    [Nullable(false)]
    public bool? PasswordExpirationEnabled { get; set; }

    [Description("Whether password policy is enforced.")]
    [Nullable(false)]
    public bool? PasswordPolicyEnforced { get; set; }

    [Description("Whether the user must change the password on next login.")]
    [Nullable(false)]
    public bool? MustChangePassword { get; set; }

    [Description("Whether to deny Windows login access. Only applicable for Windows logins.")]
    [Nullable(false)]
    public bool? DenyWindowsLogin { get; set; }

    [Description("Server roles to assign to the login. Valid roles: sysadmin, serveradmin, securityadmin, processadmin, setupadmin, bulkadmin, diskadmin, dbcreator, public.")]
    [Nullable(false)]
    [UniqueItems(true)]
    public string[]? ServerRoles { get; set; }

    [JsonPropertyName("_purge")]
    [Description("When true, removes server roles not in the ServerRoles list. When false, only adds roles from the ServerRoles list without removing others. Only applicable when ServerRoles is specified.")]
    [Nullable(false)]
    [WriteOnly]
    [Default(false)]
    public bool? Purge { get; set; }

    [ReadOnly]
    [Description("The creation date of the login.")]
    public DateTime? CreateDate { get; set; }

    [ReadOnly]
    [Description("The date the login was last modified.")]
    public DateTime? DateLastModified { get; set; }

    [ReadOnly]
    [Description("Whether the login has access to the server.")]
    public bool? HasAccess { get; set; }

    [ReadOnly]
    [Description("Whether the login is locked out.")]
    public bool? IsLocked { get; set; }

    [ReadOnly]
    [Description("Whether the login's password has expired.")]
    public bool? IsPasswordExpired { get; set; }

    [ReadOnly]
    [Description("Whether this is a system login.")]
    public bool? IsSystemObject { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the login exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
