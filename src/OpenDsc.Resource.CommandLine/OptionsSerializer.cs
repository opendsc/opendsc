// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace OpenDsc.Resource.CommandLine;

#if NET6_0_OR_GREATER
[RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
[RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
internal class OptionsSerializer<TSchema>(JsonSerializerOptions options) : ISerializer<TSchema>
{
    private readonly JsonSerializerOptions _options = options;

    public string Serialize(TSchema item) => JsonSerializer.Serialize(item, _options);
    public string SerializeManifest(DscResourceManifest manifest) => JsonSerializer.Serialize(manifest, _options);
    public string SerializeHashSet(HashSet<string> set) => JsonSerializer.Serialize(set, _options);
    public TSchema Deserialize(string json) => JsonSerializer.Deserialize<TSchema>(json, _options) ?? throw new InvalidDataException();
}
