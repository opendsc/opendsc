// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

using SmoUserType = Microsoft.SqlServer.Management.Smo.UserType;

namespace OpenDsc.Resource.SqlServer.DatabaseUser;

[Title("SQL Server Database User Schema")]
[Description("Schema for managing SQL Server database users via OpenDsc.")]
[AdditionalProperties(false)]
public sealed class Schema
{
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

    [Description("The name of the database containing the user.")]
    [Pattern(@"^[^\[\]]+$")]
    public string DatabaseName { get; set; } = string.Empty;

    [Description("The name of the database user.")]
    [Pattern(@"^.+$")]
    public string Name { get; set; } = string.Empty;

    [Description("The type of user. Determines how the user authenticates to the database.")]
    [Nullable(false)]
    public SmoUserType? UserType { get; set; }

    [Description("The name of the login to map to this database user. Required for SqlUser and AsymmetricKeyMappedLogin user types.")]
    [Nullable(false)]
    public string? Login { get; set; }

    [Description("The default schema for the user. If not specified, defaults to 'dbo'.")]
    [Nullable(false)]
    public string? DefaultSchema { get; set; }

    [ReadOnly]
    [Description("The default language for the user.")]
    public string? DefaultLanguage { get; set; }

    [Description("The password for the user. Write-only, only applicable for contained database users.")]
    [Nullable(false)]
    [WriteOnly]
    public string? Password { get; set; }

    [Description("The name of the asymmetric key associated with this user. Only applicable for AsymmetricKeyMappedUser type.")]
    [Nullable(false)]
    public string? AsymmetricKey { get; set; }

    [Description("The name of the certificate associated with this user. Only applicable for CertificateMappedUser type.")]
    [Nullable(false)]
    public string? Certificate { get; set; }

    [ReadOnly]
    [Description("The creation date of the user.")]
    public DateTime? CreateDate { get; set; }

    [ReadOnly]
    [Description("The date the user was last modified.")]
    public DateTime? DateLastModified { get; set; }

    [ReadOnly]
    [Description("Whether the user has access to the database.")]
    public bool? HasDBAccess { get; set; }

    [ReadOnly]
    [Description("Whether this is a system user.")]
    public bool? IsSystemObject { get; set; }

    [ReadOnly]
    [Description("The security identifier (SID) of the user.")]
    public string? Sid { get; set; }

    [ReadOnly]
    [Description("The authentication type of the user.")]
    public string? AuthenticationType { get; set; }

    [JsonPropertyName("_exist")]
    [Description("Indicates whether the database user exists.")]
    [Nullable(false)]
    [Default(true)]
    public bool? Exist { get; set; }
}
