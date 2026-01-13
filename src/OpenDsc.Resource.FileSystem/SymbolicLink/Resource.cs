// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.FileSystem.SymbolicLink;

[DscResource("OpenDsc.FileSystem/SymbolicLink", "0.1.0", Description = "Manage symbolic links", Tags = ["symlink", "filesystem", "link"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(SecurityException), Description = "Access denied")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[ExitCode(5, Exception = typeof(IOException), Description = "IO error")]
[ExitCode(6, Exception = typeof(UnauthorizedAccessException), Description = "Insufficient privileges")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>
{
    public override string GetSchema()
    {
        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase
        };

        var builder = new JsonSchemaBuilder().FromType<Schema>(config);
        builder.Schema("https://json-schema.org/draft/2020-12/schema");
        var schema = builder.Build();

        return JsonSerializer.Serialize(schema);
    }

    public Schema Get(Schema instance)
    {
        var schema = new Schema { Path = instance.Path };

        if (System.IO.File.Exists(instance.Path))
        {
            var fileInfo = new FileInfo(instance.Path);

            if (fileInfo.LinkTarget is not null)
            {
                schema.Target = fileInfo.LinkTarget;
                schema.Type = LinkType.File;
            }
            else
            {
                schema.Exist = false;
            }
        }
        else if (System.IO.Directory.Exists(instance.Path))
        {
            var dirInfo = new DirectoryInfo(instance.Path);

            if (dirInfo.LinkTarget is not null)
            {
                schema.Target = dirInfo.LinkTarget;
                schema.Type = LinkType.Directory;
            }
            else
            {
                schema.Exist = false;
            }
        }
        else
        {
            schema.Exist = false;
        }

        return schema;
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        if (Get(instance).Exist != false)
        {
            if (System.IO.File.Exists(instance.Path))
            {
                System.IO.File.Delete(instance.Path);
            }
            else if (System.IO.Directory.Exists(instance.Path))
            {
                System.IO.Directory.Delete(instance.Path);
            }
        }

        var linkType = instance.Type;

        if (linkType is null)
        {
            if (System.IO.File.Exists(instance.Target))
            {
                linkType = LinkType.File;
            }
            else if (System.IO.Directory.Exists(instance.Target))
            {
                linkType = LinkType.Directory;
            }
            else
            {
                throw new ArgumentException($"Cannot auto-detect link type for target '{instance.Target}'. Specify the 'type' property.");
            }
        }

        if (linkType == LinkType.File)
        {
            System.IO.File.CreateSymbolicLink(instance.Path, instance.Target);
        }
        else
        {
            System.IO.Directory.CreateSymbolicLink(instance.Path, instance.Target);
        }

        return null;
    }

    public void Delete(Schema instance)
    {
        if (Get(instance).Exist != false)
        {
            if (System.IO.File.Exists(instance.Path))
            {
                System.IO.File.Delete(instance.Path);
            }
            else if (System.IO.Directory.Exists(instance.Path))
            {
                System.IO.Directory.Delete(instance.Path);
            }
        }
    }
}
