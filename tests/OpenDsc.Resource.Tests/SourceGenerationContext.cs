// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Resource.Tests;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(TestSchema))]
[JsonSerializable(typeof(Info))]
[JsonSerializable(typeof(Warning))]
[JsonSerializable(typeof(Error))]
[JsonSerializable(typeof(Trace))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}
