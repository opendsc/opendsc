// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;

namespace OpenDsc.Resource.Windows.Shortcut;

[DscResource("OpenDsc.Windows/Shortcut", "0.1.0", Description = "Manage Windows shortcuts", Tags = ["shortcut", "windows"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(InvalidOperationException), Description = "Failed to generate schema")]
[ExitCode(4, Exception = typeof(DirectoryNotFoundException), Description = "Directory not found")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>
{
    public override string GetSchema()
    {
        var registry = new SchemaRegistry();
        var schema = registry.CreateBundle(GeneratedJsonSchemas.Shortcut_Schema.BaseUri, Schema.BundleUri);
        return JsonSerializer.Serialize(schema, SourceGenerationContext.Default.JsonSchema);
    }

    public Schema Get(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (!File.Exists(instance.Path))
        {
            return new Schema()
            {
                Path = instance.Path,
                Exist = false
            };
        }

        return ShortcutHelper.ReadShortcut(instance.Path);
    }

    public void Delete(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (File.Exists(instance.Path))
        {
            File.Delete(instance.Path);
        }
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        string? directoryName = Path.GetDirectoryName(instance.Path);
        if (directoryName == null || !Directory.Exists(directoryName))
        {
            throw new DirectoryNotFoundException($"The directory for the shortcut path '{instance.Path}' does not exist.");
        }

        ShortcutHelper.CreateShortcut(instance);
        return null;
    }
}
