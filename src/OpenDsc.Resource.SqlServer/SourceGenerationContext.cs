// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Resource.SqlServer;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Login.Schema), TypeInfoPropertyName = "LoginSchema")]
[JsonSerializable(typeof(Database.Schema), TypeInfoPropertyName = "DatabaseSchema")]
[JsonSerializable(typeof(DatabaseRole.Schema), TypeInfoPropertyName = "DatabaseRoleSchema")]
[JsonSerializable(typeof(ServerRole.Schema), TypeInfoPropertyName = "ServerRoleSchema")]
[JsonSerializable(typeof(DatabasePermission.Schema), TypeInfoPropertyName = "DatabasePermissionSchema")]
[JsonSerializable(typeof(ServerPermission.Schema), TypeInfoPropertyName = "ServerPermissionSchema")]
[JsonSerializable(typeof(Configuration.Schema), TypeInfoPropertyName = "ConfigurationSchema")]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
