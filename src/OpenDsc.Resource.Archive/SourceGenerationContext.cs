// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Resource.Archive;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Zip.Compress.Schema), TypeInfoPropertyName = "ZipCompressSchema")]
[JsonSerializable(typeof(Zip.Expand.Schema), TypeInfoPropertyName = "ZipExpandSchema")]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
