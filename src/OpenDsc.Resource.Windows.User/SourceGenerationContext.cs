// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;
using OpenDsc.Resource;
using OpenDsc.Resource.CommandLine;

namespace OpenDsc.Resource.Windows.User;

[JsonSourceGenerationOptions(WriteIndented = false,
                             PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                             UseStringEnumConverter = true,
                             DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                             Converters = [typeof(ResourceConverter<Schema>)])]
[JsonSerializable(typeof(IDscResource<Schema>))]
[JsonSerializable(typeof(Schema))]
[JsonSerializable(typeof(HashSet<string>))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
