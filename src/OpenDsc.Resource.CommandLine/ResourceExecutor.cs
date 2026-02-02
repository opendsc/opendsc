// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

namespace OpenDsc.Resource.CommandLine;

internal static class ResourceExecutor<TResource, TSchema> where TResource : IDscResource<TSchema>
{
    internal static void ExecuteSchema(IDscResource<TSchema> resource)
    {
        Console.WriteLine(resource.GetSchema());
    }

    internal static void ExecuteManifest(IDscResource<TSchema> resource)
    {
        var manifest = ManifestBuilder.Build(resource);
        var json = JsonSerializer.Serialize(manifest, typeof(DscResourceManifest), SourceGenerationContext.Default);
        Console.WriteLine(json);
    }

    internal static void ExecuteGet(IDscResource<TSchema> resource, string? inputOption)
    {
        TSchema? instance = inputOption is not null ? resource.Parse(inputOption) : default;

        if (resource is not IGettable<TSchema> iGettable)
        {
            throw new NotImplementedException("Resource does not support Get capability.");
        }

        var result = iGettable.Get(instance);
        Console.WriteLine(resource.ToJson(result));
    }

    internal static void ExecuteSet(IDscResource<TSchema> resource, string? inputOption)
    {
        TSchema? instance = inputOption is not null ? resource.Parse(inputOption) : default;

        if (resource is not ISettable<TSchema> iSettable)
        {
            throw new NotImplementedException("Resource does not support Set capability.");
        }

        var result = iSettable.Set(instance);
        var dscAttr = GetDscAttribute(resource);

        if (result is not null && dscAttr.SetReturn != SetReturn.None)
        {
            var json = resource.ToJson(result.ActualState);
            Console.WriteLine(json);
        }

        if (result?.ChangedProperties is not null && dscAttr.SetReturn == SetReturn.StateAndDiff)
        {
            var json = JsonSerializer.Serialize(result.ChangedProperties, typeof(HashSet<string>), SourceGenerationContext.Default);
            Console.WriteLine(json);
        }
    }

    internal static void ExecuteTest(IDscResource<TSchema> resource, string? inputOption)
    {
        TSchema? instance = inputOption is not null ? resource.Parse(inputOption) : default;

        if (resource is not ITestable<TSchema> iTestable)
        {
            throw new NotImplementedException("Resource does not support Test capability.");
        }

        var result = iTestable.Test(instance);
        var json = resource.ToJson(result.ActualState);
        Console.WriteLine(json);

        var dscAttr = GetDscAttribute(resource);

        if (result?.DifferingProperties is not null && dscAttr.TestReturn == TestReturn.StateAndDiff)
        {
            json = JsonSerializer.Serialize(result.DifferingProperties, typeof(HashSet<string>), SourceGenerationContext.Default);
            Console.WriteLine(json);
        }
    }

    internal static void ExecuteDelete(IDscResource<TSchema> resource, string? inputOption)
    {
        TSchema? instance = inputOption is not null ? resource.Parse(inputOption) : default;

        if (resource is not IDeletable<TSchema> iTDeletable)
        {
            throw new NotImplementedException("Resource does not support Delete capability.");
        }

        iTDeletable.Delete(instance);
    }

    internal static void ExecuteExport(IDscResource<TSchema> resource, string? inputOption)
    {
        TSchema? filter = inputOption is not null ? resource.Parse(inputOption) : default;

        if (resource is not IExportable<TSchema> iExportable)
        {
            throw new NotImplementedException("Resource does not support Export capability.");
        }

        foreach (var item in iExportable.Export(filter))
        {
            Console.WriteLine(resource.ToJson(item));
        }
    }

    private static DscResourceAttribute GetDscAttribute(IDscResource<TSchema> resource)
    {
        return resource.GetType().GetCustomAttribute<DscResourceAttribute>()
                      ?? throw new InvalidOperationException($"Resource does not have '{nameof(DscResourceAttribute)}' attribute.");
    }
}
