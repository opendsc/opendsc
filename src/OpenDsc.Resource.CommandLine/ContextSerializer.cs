// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace OpenDsc.Resource.CommandLine;

internal class ContextSerializer<TSchema> : ISerializer<TSchema>
{
    private readonly JsonSerializerContext _context;

    public ContextSerializer(JsonSerializerContext context) => _context = context;

    public string Serialize(TSchema item) => JsonSerializer.Serialize(item, typeof(TSchema), _context);
    public string SerializeManifest(DscResourceManifest manifest) => JsonSerializer.Serialize(manifest, typeof(DscResourceManifest), SourceGenerationContext.Default);
    public string SerializeHashSet(HashSet<string> set) => JsonSerializer.Serialize(set, typeof(HashSet<string>), SourceGenerationContext.Default);
    public TSchema Deserialize(string json) => (TSchema)(JsonSerializer.Deserialize(json, typeof(TSchema), _context) ?? throw new InvalidDataException());
}
