// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace OpenDsc.Resource;

public abstract class AotDscResource<T> : DscResourceBase<T>
{
    protected JsonSerializerContext Context { get; set; }

    protected AotDscResource(string type, JsonSerializerContext context) : base(type)
    {
        Context = context;
    }

    public override string GetSchema()
    {
        var typeInfo = Context.GetTypeInfo(typeof(T));

        if (typeInfo is null)
        {
            throw new ArgumentException();
        }

        return JsonSchemaExporter.GetJsonSchemaAsNode(typeInfo, ExporterOptions).ToJsonString();
    }

    public override T Parse(string json)
    {
        var typeInfo = Context.GetTypeInfo(typeof(T));

        if (typeInfo is null)
        {
            throw new ArgumentException();
        }

        return JsonSerializer.Deserialize<T>(json, (JsonTypeInfo<T>)typeInfo) ?? throw new InvalidDataException();
    }

    public override string ToJson(T instance)
    {
        return JsonSerializer.Serialize(instance, typeof(T), Context);
    }

}
