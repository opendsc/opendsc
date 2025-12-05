// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace OpenDsc.Resource;

public abstract class DscResource<T> : IDscResource<T>
{
    private readonly JsonSerializerOptions? _serializerOptions;
    private readonly JsonSerializerContext? _context;
    private readonly JsonSchemaExporterOptions _exporterOptions;

    public DscResource(JsonSerializerOptions options)
    {
        _serializerOptions = options;
        _exporterOptions = DscJsonSchemaExporterOptions.Default;
    }

    public DscResource(JsonSerializerContext context)
    {
        _context = context;
        _exporterOptions = DscJsonSchemaExporterOptions.Default;
    }

    protected virtual JsonTypeInfo<T> GetTypeInfo()
    {
        return _context is not null ?
            (JsonTypeInfo<T>)(_context.GetTypeInfo(typeof(T)) ?? throw new ArgumentException($"Unable to get type info for {typeof(T).FullName} from the provided JsonSerializerContext.")) :
            (JsonTypeInfo<T>)_serializerOptions!.GetTypeInfo(typeof(T));
    }

    public virtual string GetSchema()
    {
        var typeInfo = GetTypeInfo();
        return JsonSchemaExporter.GetJsonSchemaAsNode(typeInfo, _exporterOptions).ToJsonString();
    }

    public virtual T Parse(string json)
    {
        return JsonSerializer.Deserialize(json, GetTypeInfo()) ?? throw new InvalidDataException();
    }

    public virtual string ToJson(T item)
    {
        return JsonSerializer.Serialize(item, GetTypeInfo());
    }
}
