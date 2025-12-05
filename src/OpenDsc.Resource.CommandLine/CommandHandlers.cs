// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;

namespace OpenDsc.Resource.CommandLine;

internal static class CommandHandlers<TResource, TSchema> where TResource : IDscResource<TSchema>
{
    public static void SchemaHandler(IDscResource<TSchema> resource)
    {
        Console.WriteLine(resource.GetSchema());
    }

    public static void ManifestHandler(IDscResource<TSchema> resource, ISerializer<TSchema> serializer)
    {
        var manifest = ManifestBuilder.Build(resource);
        var json = serializer.SerializeManifest(manifest);
        Console.WriteLine(json);
    }

    public static void GetHandler(IDscResource<TSchema> resource, string inputOption)
    {
        var instance = resource.Parse(inputOption);

        if (resource is not IGettable<TSchema> iGettable)
        {
            throw new NotImplementedException("Resource does not support Get capability.");
        }

        var result = iGettable.Get(instance);
        Console.WriteLine(resource.ToJson(result));
    }

    public static void SetHandler(IDscResource<TSchema> resource, string inputOption, ISerializer<TSchema> serializer)
    {
        var instance = resource.Parse(inputOption);

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
            var json = serializer.SerializeHashSet(result.ChangedProperties);
            Console.WriteLine(json);
        }
    }

    public static void TestHandler(IDscResource<TSchema> resource, string inputOption, ISerializer<TSchema> serializer)
    {
        var instance = resource.Parse(inputOption);

        if (resource is not ITestable<TSchema> iTestable)
        {
            throw new NotImplementedException("Resource does not support Test capability.");
        }

        var result = iTestable.Test(instance);
        var json = serializer.Serialize(result.ActualState);
        Console.WriteLine(json);

        var dscAttr = GetDscAttribute(resource);

        if (result?.DifferingProperties is not null && dscAttr.TestReturn == TestReturn.StateAndDiff)
        {
            json = serializer.SerializeHashSet(result.DifferingProperties);
            Console.WriteLine(json);
        }
    }

    public static void DeleteHandler(IDscResource<TSchema> resource, string inputOption)
    {
        var instance = resource.Parse(inputOption);

        if (resource is not IDeletable<TSchema> iTDeletable)
        {
            throw new NotImplementedException("Resource does not support Delete capability.");
        }

        iTDeletable.Delete(instance);
    }

    public static void ExportHandler(IDscResource<TSchema> resource)
    {
        if (resource is not IExportable<TSchema> iExportable)
        {
            throw new NotImplementedException("Resource does not support Export capability.");
        }

        foreach (var item in iExportable.Export())
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
