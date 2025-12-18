#if (!use-options)
using System.Text.Json.Serialization;

using OpenDsc.Resource;

namespace Temp;

[JsonSourceGenerationOptions(WriteIndented = false,
                             PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                             UseStringEnumConverter = true,
                             DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Schema))]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
#endif
