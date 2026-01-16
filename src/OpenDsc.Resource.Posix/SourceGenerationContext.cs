// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Resource.Posix;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(FileSystem.Permission.Schema), TypeInfoPropertyName = "FileSystemPermissionSchema")]
[JsonSerializable(typeof(Cron.Job.Schema), TypeInfoPropertyName = "CronJobSchema")]
[JsonSerializable(typeof(Cron.Environment.Schema), TypeInfoPropertyName = "CronEnvironmentSchema")]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
