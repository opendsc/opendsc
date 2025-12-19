// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace TestResource.Multi;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(FileSchema))]
[JsonSerializable(typeof(UserSchema))]
[JsonSerializable(typeof(ServiceSchema))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
