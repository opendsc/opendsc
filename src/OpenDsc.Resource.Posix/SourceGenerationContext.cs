// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using Json.Schema;

namespace OpenDsc.Resource.Posix;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(FileSystem.Permission.Schema), TypeInfoPropertyName = "FileSystemPermissionSchema")]
[JsonSerializable(typeof(JsonSchema), TypeInfoPropertyName = "JsonSchema")]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
