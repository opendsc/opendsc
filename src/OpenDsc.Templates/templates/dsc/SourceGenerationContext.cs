using System.Text.Json.Serialization;

using OpenDsc.Resource;
using OpenDsc.Resource.CommandLine;

namespace Temp;

[JsonSourceGenerationOptions(WriteIndented = false,
                             PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                             UseStringEnumConverter = true,
                             DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                             Converters = [typeof(ResourceConverter<TempSchema>)])]
[JsonSerializable(typeof(IDscResource<TempSchema>))]
[JsonSerializable(typeof(TempSchema))]
[JsonSerializable(typeof(HashSet<string>))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
