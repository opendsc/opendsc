// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Resource.CommandLine;

[JsonSourceGenerationOptions(WriteIndented = false,
                             PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                             UseStringEnumConverter = true,
                             DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DscResourceManifest))]
[JsonSerializable(typeof(ManifestSchema))]
[JsonSerializable(typeof(ManifestMethod))]
[JsonSerializable(typeof(ManifestSetMethod))]
[JsonSerializable(typeof(ManifestTestMethod))]
[JsonSerializable(typeof(ManifestExportMethod))]
[JsonSerializable(typeof(JsonInputArg))]
[JsonSerializable(typeof(HashSet<string>))]
[JsonSerializable(typeof(MultiResourceManifest))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
