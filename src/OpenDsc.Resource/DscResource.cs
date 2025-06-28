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

#if NET6_0_OR_GREATER
    [RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
public abstract class DscResource<T> : DscResourceBase<T>
{
    protected JsonSerializerOptions SerializerOptions
    {
        get
        {
            _serializerOptions ??= new JsonSerializerOptions()
            {
                // DSC requires JSON lines for most output
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                }
            };

            return _serializerOptions;
        }

        set
        {
            _serializerOptions = value;
        }
    }

    private JsonSerializerOptions? _serializerOptions;

    protected DscResource(string type) : base(type) { }

    public override string GetSchema()
    {
        return SerializerOptions.GetJsonSchemaAsNode(typeof(T), ExporterOptions).ToString();
    }

    public override T Parse(string json)
    {
        return JsonSerializer.Deserialize<T>(json, SerializerOptions) ?? throw new InvalidDataException();
    }

    public override string ToJson(T item)
    {
        return JsonSerializer.Serialize(item, SerializerOptions);
    }
}
