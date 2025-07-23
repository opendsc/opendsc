using System.Text.Json.Serialization;

using OpenDsc.Resource;
using OpenDsc.Resource.CommandLine;

namespace Temp;

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
