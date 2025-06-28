// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

#if NET6_0_OR_GREATER
using System.Text.Json.Serialization;

namespace OpenDsc.Resource;

[JsonSourceGenerationOptions(WriteIndented = false,
                             PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                             UseStringEnumConverter = true,
                             UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                             GenerationMode = JsonSourceGenerationMode.Serialization)]
[JsonSerializable(typeof(Info))]
[JsonSerializable(typeof(Warning))]
[JsonSerializable(typeof(Error))]
[JsonSerializable(typeof(Trace))]
[JsonSerializable(typeof(string[]))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
#endif
