// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;
using OpenDsc.Resource;

namespace TestResource.Multi;

public sealed class FileSchema
{
    public required string Path { get; set; }
    public string? Content { get; set; }
    [JsonPropertyName("_exist")]
    public bool? Exist { get; set; }
}

[DscResource("TestResource.Multi/File", "1.0.0", Description = "Manages file content", Tags = ["file", "content"], SetReturn = SetReturn.State, TestReturn = TestReturn.State)]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Description = "Invalid parameter")]
[ExitCode(2, Exception = typeof(Exception), Description = "Unhandled error")]
public sealed class FileResource(JsonSerializerContext context) : DscResource<FileSchema>(context),
    IGettable<FileSchema>, ISettable<FileSchema>, ITestable<FileSchema>, IDeletable<FileSchema>
{
    public FileSchema Get(FileSchema instance)
    {
        var exists = File.Exists(instance.Path);
        return new FileSchema
        {
            Path = instance.Path,
            Content = exists ? File.ReadAllText(instance.Path) : null,
            Exist = exists == false ? false : null
        };
    }

    public SetResult<FileSchema>? Set(FileSchema instance)
    {
        var current = Get(instance);
        var changedProperties = new List<string>();

        if (instance.Exist == false)
        {
            if (File.Exists(instance.Path))
            {
                File.Delete(instance.Path);
                changedProperties.Add(nameof(FileSchema.Exist));
            }
        }
        else
        {
            if (!File.Exists(instance.Path) || current.Content != instance.Content)
            {
                File.WriteAllText(instance.Path, instance.Content ?? string.Empty);
                changedProperties.Add(nameof(FileSchema.Content));
                if (current.Exist != true)
                {
                    changedProperties.Add(nameof(FileSchema.Exist));
                }
            }
        }

        var actualState = Get(instance);
        return new SetResult<FileSchema>(actualState)
        {
            ChangedProperties = changedProperties.Count > 0 ? [.. changedProperties] : null
        };
    }

    public TestResult<FileSchema> Test(FileSchema instance)
    {
        var current = Get(instance);
        var differingProperties = new List<string>();

        if (instance.Exist != current.Exist)
        {
            differingProperties.Add(nameof(FileSchema.Exist));
        }

        if (instance.Exist != false && instance.Content != current.Content)
        {
            differingProperties.Add(nameof(FileSchema.Content));
        }

        return new TestResult<FileSchema>(current)
        {
            DifferingProperties = differingProperties.Count > 0 ? [.. differingProperties] : null
        };
    }

    public void Delete(FileSchema instance)
    {
        if (File.Exists(instance.Path))
        {
            File.Delete(instance.Path);
        }
    }
}
