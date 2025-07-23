using System.Text.Json;

using OpenDsc.Resource;

namespace Temp;

[DscResource("InsertOwner/Temp", Description = "Insert description", Tags = ["tag1", "tag2"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Description = "Invalid parameter")]
[ExitCode(2, Exception = typeof(Exception), Description = "Generic error")]
[ExitCode(3, Exception = typeof(JsonException), Description = "Invalid JSON")]
public sealed class Resource : DscResource<Schema>, IGettable<Schema>
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
