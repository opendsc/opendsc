#if (!use-options)
using System.Text.Json.Serialization;

using Json.Schema;

using OpenDsc.Resource;

namespace Temp;

[JsonSourceGenerationOptions(WriteIndented = false,
                             PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                             UseStringEnumConverter = true,
                             DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Schema))]
[JsonSerializable(typeof(JsonSchema), TypeInfoPropertyName = "JsonSchema")]
internal partial class SourceGenerationContext : JsonSerializerContext
{

}
#endif
