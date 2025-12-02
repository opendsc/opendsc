using System.Text.Json;
#if (use-options)
#else
using System.Text.Json.Serialization;
#endif

using OpenDsc.Resource;

namespace Temp;

[DscResource("RESOURCE_NAME", Description = "RESOURCE_DESCRIPTION", Tags = new[] { "tag1", "tag2" })]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Description = "Invalid parameter")]
[ExitCode(2, Exception = typeof(Exception), Description = "Generic error")]
[ExitCode(3, Exception = typeof(JsonException), Description = "Invalid JSON")]
#if (use-options)
public sealed class Resource(JsonSerializerOptions options) : DscResource<Schema>(options), IGettable<Schema>
#else
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>
#endif
{
    public Schema Get(Schema instance)
    {
        return new Schema()
        {
            Name = "Test",
            Exist = false
        };
    }
}
