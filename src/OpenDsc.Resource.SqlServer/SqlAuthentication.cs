// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer;

/// <summary>
/// Specifies the authentication method to use when connecting to SQL Server.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<SqlAuthType>))]
public enum SqlAuthType
{
    /// <summary>
    /// Use Windows Authentication (Integrated Security).
    /// </summary>
    Windows,

    /// <summary>
    /// Use SQL Server Authentication with username and password.
    /// </summary>
    Sql
}

/// <summary>
/// Authentication configuration for SQL Server connections.
/// </summary>
[Title("SQL Server Authentication")]
[Description("Authentication settings for connecting to SQL Server.")]
[AdditionalProperties(false)]
public sealed class SqlAuthentication
{
    /// <summary>
    /// The type of authentication to use.
    /// </summary>
    [Required]
    [Description("The authentication type. 'Windows' uses Integrated Security, 'Sql' requires username and password.")]
    public SqlAuthType AuthType { get; set; }

    /// <summary>
    /// The username for SQL Server authentication.
    /// </summary>
    [Description("The username for SQL Server authentication. Required when authType is 'Sql'.")]
    [Nullable(false)]
    public string? Username { get; set; }

    /// <summary>
    /// The password for SQL Server authentication.
    /// </summary>
    [Description("The password for SQL Server authentication. Required when authType is 'Sql'.")]
    [WriteOnly]
    [Nullable(false)]
    public string? Password { get; set; }
}
