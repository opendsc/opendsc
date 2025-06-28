// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ServiceProcess;
using System.Text.Json.Serialization;

using OpenDsc.Resource.CommandLine;

namespace OpenDsc.Resource.Windows.Service;

[JsonSourceGenerationOptions(WriteIndented = false,
                             PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                             UseStringEnumConverter = true,
                             DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                             Converters = [typeof(ResourceConverter<Schema>)])]
[JsonSerializable(typeof(Schema))]
[JsonSerializable(typeof(IDscResource<Schema>))]
[JsonSerializable(typeof(HashSet<string>))]
[JsonSerializable(typeof(ServiceControllerStatus))]
[JsonSerializable(typeof(ServiceStartMode))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
