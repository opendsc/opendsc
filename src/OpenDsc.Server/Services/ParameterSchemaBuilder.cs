// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;

namespace OpenDsc.Server.Services;

public interface IParameterSchemaBuilder
{
    /// <summary>
    /// Builds JSON Schema from DSC parameters block for validation.
    /// </summary>
    JsonSchema BuildJsonSchema(Dictionary<string, ParameterDefinition> parametersBlock);

    /// <summary>
    /// Serializes JSON Schema to string for storage.
    /// </summary>
    string SerializeSchema(JsonSchema schema);
}

public sealed class ParameterSchemaBuilder : IParameterSchemaBuilder
{
    public JsonSchema BuildJsonSchema(Dictionary<string, ParameterDefinition> parametersBlock)
    {
        // Build inner schema for actual parameter properties
        var innerBuilder = new JsonSchemaBuilder()
            .Type(SchemaValueType.Object);

        var properties = new Dictionary<string, JsonSchemaBuilder>();
        var required = new List<string>();

        foreach (var (paramName, paramDef) in parametersBlock)
        {
            var paramSchemaBuilder = BuildParameterSchemaBuilder(paramDef);
            properties[paramName] = paramSchemaBuilder;

            // Parameter is required if it has no defaultValue
            if (paramDef.DefaultValue == null)
            {
                required.Add(paramName);
            }
        }

        innerBuilder.Properties(properties);

        if (required.Count > 0)
        {
            innerBuilder.Required(required);
        }

        innerBuilder.AdditionalProperties(false);

        // Wrap in root "parameters" object structure to match DSC parameter file format
        // Parameter files have structure: { parameters: { param1: value1, ... } }
        var rootBuilder = new JsonSchemaBuilder()
            .Schema("https://json-schema.org/draft/2020-12/schema")
            .Type(SchemaValueType.Object)
            .Properties(new Dictionary<string, JsonSchemaBuilder>
            {
                ["parameters"] = innerBuilder
            })
            .Required(new[] { "parameters" })
            .AdditionalProperties(false);

        return rootBuilder.Build();
    }

    public string SerializeSchema(JsonSchema schema)
    {
        return JsonSerializer.Serialize(schema, new JsonSerializerOptions
        {
            WriteIndented = false
        });
    }

    private static JsonSchemaBuilder BuildParameterSchemaBuilder(ParameterDefinition param)
    {
        var builder = new JsonSchemaBuilder();
        var normalizedType = param.Type.ToLowerInvariant();

        ApplyType(builder, normalizedType, param.Type);
        ApplyDescription(builder, param.Description);
        ApplyAllowedValues(builder, param.AllowedValues);
        ApplyTypeConstraints(builder, normalizedType, param);

        return builder;
    }

    private static void ApplyType(JsonSchemaBuilder builder, string normalizedType, string originalType)
    {
        // Map DSC parameter type to JSON Schema type (case-insensitive to match spec)
        builder.Type(normalizedType switch
        {
            "string" or "securestring" => SchemaValueType.String,
            "int" => SchemaValueType.Integer,
            "bool" => SchemaValueType.Boolean,
            "object" or "secureobject" => SchemaValueType.Object,
            "array" => SchemaValueType.Array,
            _ => throw new ArgumentException($"Unknown parameter type: {originalType}")
        });
    }

    private static void ApplyDescription(JsonSchemaBuilder builder, string? description)
    {
        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.Description(description);
        }
    }

    private static void ApplyAllowedValues(JsonSchemaBuilder builder, object[]? allowedValues)
    {
        if (allowedValues != null && allowedValues.Length > 0)
        {
            builder.Enum(allowedValues.Select(v => JsonSerializer.SerializeToNode(v)).ToArray()!);
        }
    }

    private static void ApplyTypeConstraints(JsonSchemaBuilder builder, string normalizedType, ParameterDefinition param)
    {
        if (normalizedType is "string" or "securestring")
        {
            ApplyLengthConstraints(builder, param.MinLength, param.MaxLength);
        }
        else if (normalizedType == "array")
        {
            ApplyArrayItemConstraints(builder, param.MinLength, param.MaxLength);
        }

        if (normalizedType == "int")
        {
            ApplyNumericConstraints(builder, param.MinValue, param.MaxValue);
        }
    }

    private static void ApplyLengthConstraints(JsonSchemaBuilder builder, int? minLength, int? maxLength)
    {
        if (minLength.HasValue)
        {
            builder.MinLength((uint)minLength.Value);
        }

        if (maxLength.HasValue)
        {
            builder.MaxLength((uint)maxLength.Value);
        }
    }

    private static void ApplyArrayItemConstraints(JsonSchemaBuilder builder, int? minItems, int? maxItems)
    {
        if (minItems.HasValue)
        {
            builder.MinItems((uint)minItems.Value);
        }

        if (maxItems.HasValue)
        {
            builder.MaxItems((uint)maxItems.Value);
        }
    }

    private static void ApplyNumericConstraints(JsonSchemaBuilder builder, int? minValue, int? maxValue)
    {
        if (minValue.HasValue)
        {
            builder.Minimum(minValue.Value);
        }

        if (maxValue.HasValue)
        {
            builder.Maximum(maxValue.Value);
        }
    }
}

/// <summary>
/// Represents a DSC parameter definition from the parameters block.
/// </summary>
public sealed class ParameterDefinition
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("defaultValue")]
    public object? DefaultValue { get; set; }

    [JsonPropertyName("allowedValues")]
    public object[]? AllowedValues { get; set; }

    [JsonPropertyName("minLength")]
    public int? MinLength { get; set; }

    [JsonPropertyName("maxLength")]
    public int? MaxLength { get; set; }

    [JsonPropertyName("minValue")]
    public int? MinValue { get; set; }

    [JsonPropertyName("maxValue")]
    public int? MaxValue { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }
}
