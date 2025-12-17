// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using OpenDsc.Resource;

namespace TestResource.Multi;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(FileSchema))]
[JsonSerializable(typeof(TestResult<FileSchema>))]
[JsonSerializable(typeof(SetResult<FileSchema>))]
[JsonSerializable(typeof(UserSchema))]
[JsonSerializable(typeof(TestResult<UserSchema>))]
[JsonSerializable(typeof(SetResult<UserSchema>))]
[JsonSerializable(typeof(ServiceSchema))]
[JsonSerializable(typeof(TestResult<ServiceSchema>))]
[JsonSerializable(typeof(SetResult<ServiceSchema>))]
[JsonSerializable(typeof(DscResourceManifest))]
[JsonSerializable(typeof(JsonElement))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
