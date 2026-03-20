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

        // Map DSC parameter type to JSON Schema type
        builder.Type(param.Type switch
        {
            "string" => SchemaValueType.String,
            "secureString" => SchemaValueType.String,
            "int" => SchemaValueType.Integer,
            "bool" => SchemaValueType.Boolean,
            "object" => SchemaValueType.Object,
            "secureObject" => SchemaValueType.Object,
            "array" => SchemaValueType.Array,
            _ => throw new ArgumentException($"Unknown parameter type: {param.Type}")
        });

        // Add description if present
        if (!string.IsNullOrWhiteSpace(param.Description))
        {
            builder.Description(param.Description);
        }

        // Apply constraints based on type
        if (param.AllowedValues != null && param.AllowedValues.Length > 0)
        {
            builder.Enum(param.AllowedValues.Select(v => JsonSerializer.SerializeToNode(v)).ToArray()!);
        }

        if (param.Type is "string" or "secureString" or "array")
        {
            if (param.MinLength.HasValue)
            {
                builder.MinLength((uint)param.MinLength.Value);
            }

            if (param.MaxLength.HasValue)
            {
                builder.MaxLength((uint)param.MaxLength.Value);
            }
        }

        if (param.Type == "int")
        {
            if (param.MinValue.HasValue)
            {
                builder.Minimum(param.MinValue.Value);
            }

            if (param.MaxValue.HasValue)
            {
                builder.Maximum(param.MaxValue.Value);
            }
        }

        return builder;
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
