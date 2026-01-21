// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace OpenDsc.Resource;

/// <summary>
/// Base class for implementing DSC (Desired State Configuration) resources.
/// </summary>
/// <typeparam name="T">The schema type that defines the resource's properties.</typeparam>
public abstract class DscResource<T> : IDscResource<T>
{
    private readonly JsonSerializerOptions? _serializerOptions;
    private readonly JsonSerializerContext? _context;
    private readonly JsonSchemaExporterOptions _exporterOptions;

    /// <summary>
    /// Gets the logger instance for this resource.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DscResource{T}"/> class with JSON serializer options.
    /// </summary>
    /// <param name="options">The JSON serializer options for serialization operations.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public DscResource(JsonSerializerOptions options, ILogger? logger = null)
    {
        _serializerOptions = options;
        _exporterOptions = DscJsonSchemaExporterOptions.Default;
        Logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DscResource{T}"/> class with a JSON serializer context.
    /// This constructor is recommended for Native AOT compilation.
    /// </summary>
    /// <param name="context">The JSON serializer context for serialization operations.</param>
    /// <param name="logger">Optional logger instance for diagnostic messages.</param>
    public DscResource(JsonSerializerContext context, ILogger? logger = null)
    {
        _context = context;
        _exporterOptions = DscJsonSchemaExporterOptions.Default;
        Logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Gets the JSON type information for the schema type.
    /// </summary>
    /// <returns>The JSON type information for serialization.</returns>
    protected virtual JsonTypeInfo<T> GetTypeInfo()
    {
        return _context is not null ?
            (JsonTypeInfo<T>)(_context.GetTypeInfo(typeof(T)) ?? throw new ArgumentException($"Unable to get type info for {typeof(T).FullName} from the provided JsonSerializerContext.")) :
            (JsonTypeInfo<T>)_serializerOptions!.GetTypeInfo(typeof(T));
    }

    /// <summary>
    /// Gets the JSON schema for the resource.
    /// </summary>
    /// <returns>A JSON string representing the resource schema.</returns>
    public virtual string GetSchema()
    {
        var typeInfo = GetTypeInfo();
        return JsonSchemaExporter.GetJsonSchemaAsNode(typeInfo, _exporterOptions).ToJsonString();
    }

    /// <summary>
    /// Parses a JSON string into a resource instance.
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>The deserialized resource instance.</returns>
    /// <exception cref="InvalidDataException">Thrown when the JSON cannot be deserialized.</exception>
    public virtual T Parse(string json)
    {
        return JsonSerializer.Deserialize(json, GetTypeInfo()) ?? throw new InvalidDataException();
    }

    /// <summary>
    /// Serializes a resource instance to a JSON string.
    /// </summary>
    /// <param name="item">The resource instance to serialize.</param>
    /// <returns>A JSON string representation of the resource instance.</returns>
    public virtual string ToJson(T item)
    {
        return JsonSerializer.Serialize(item, GetTypeInfo());
    }
}
