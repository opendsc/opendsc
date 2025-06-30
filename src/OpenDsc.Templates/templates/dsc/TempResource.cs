using OpenDsc.Resource;

namespace Temp;

public sealed class TempResource : DscResource<TempSchema>, IGettable<TempSchema>
{
    public TempResource() : base("Temp")
    {
        Description = "Manage files.";
        Tags = ["Windows"];
        ExitCodes.Add(10, new() { Exception = typeof(FileNotFoundException), Description = "File not found" });
    }

    public TempSchema Get(TempSchema instance)
    {
        return new TempSchema()
        {
            Name = "Test",
            Exist = false
        };
    }
}
