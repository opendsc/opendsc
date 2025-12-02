// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using OpenDsc.Resource;

namespace TestResource.NonAot;

[JsonSourceGenerationOptions(WriteIndented = false,
                             PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                             UseStringEnumConverter = true,
                             DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Schema))]
[JsonSerializable(typeof(TestResult<Schema>))]
[JsonSerializable(typeof(SetResult<Schema>))]
[JsonSerializable(typeof(List<string>))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
