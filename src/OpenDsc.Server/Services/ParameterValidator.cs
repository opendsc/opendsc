// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;

using Json.Schema;

using YamlDotNet.Serialization;

namespace OpenDsc.Server.Services;

public interface IParameterValidator
{
    /// <summary>
    /// Validates parameter file content against a JSON Schema.
    /// </summary>
    ValidationResult Validate(string jsonSchemaString, string parameterFileContent);
}

public sealed class ParameterValidator : IParameterValidator
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithAttemptingUnquotedStringTypeDeserialization()
        .Build();

    public ValidationResult Validate(string jsonSchemaString, string parameterFileContent)
    {
        try
        {
            // Deserialize JSON Schema
            var schemaNode = JsonNode.Parse(jsonSchemaString);
            if (schemaNode == null)
            {
                return ValidationResult.Failure(new ValidationError
                {
                    Path = "$schema",
                    Message = "Invalid JSON Schema",
                    Code = "invalid_schema"
                });
            }

            var schema = JsonSerializer.Deserialize<JsonSchema>(schemaNode);
            if (schema == null)
            {
                return ValidationResult.Failure(new ValidationError
                {
                    Path = "$schema",
                    Message = "Failed to parse JSON Schema",
                    Code = "schema_parse_error"
                });
            }

            // Parse parameter file (YAML or JSON)
            JsonNode? paramContent;
            try
            {
                // Try JSON first
                paramContent = JsonNode.Parse(parameterFileContent);
            }
            catch
            {
                // Fall back to YAML
                try
                {
                    var yamlObject = YamlDeserializer.Deserialize<object>(parameterFileContent);
                    var jsonString = JsonSerializer.Serialize(yamlObject);
                    paramContent = JsonNode.Parse(jsonString);
                }
                catch (Exception ex)
                {
                    return ValidationResult.Failure(new ValidationError
                    {
                        Path = "$",
                        Message = $"Failed to parse parameter file content: {ex.Message}",
                        Code = "parse_error"
                    });
                }
            }

            if (paramContent == null)
            {
                return ValidationResult.Failure(new ValidationError
                {
                    Path = "$",
                    Message = "Parameter file content is empty",
                    Code = "empty_content"
                });
            }

            // Convert JsonNode to JsonElement for validation
            var parameterJson = paramContent.ToJsonString();
            using var doc = JsonDocument.Parse(parameterJson);
            var element = doc.RootElement.Clone();

            // Validate against schema
            var validationResults = schema.Evaluate(element, new EvaluationOptions
            {
                OutputFormat = OutputFormat.List
            });

            if (validationResults.IsValid)
            {
                return ValidationResult.Success();
            }

            // Convert JSON Schema validation errors to our format
            var errors = new List<ValidationError>();
            CollectErrors(validationResults, errors);

            return ValidationResult.Failure(errors.ToArray());
        }
        catch (Exception ex)
        {
            return ValidationResult.Failure(new ValidationError
            {
                Path = "$",
                Message = $"Validation error: {ex.Message}",
                Code = "validation_error"
            });
        }
    }

    private static void CollectErrors(EvaluationResults results, List<ValidationError> errors)
    {
        if (!results.IsValid && results.Details != null)
        {
            foreach (var detail in results.Details)
            {
                if (detail.Errors != null)
                {
                    foreach (var error in detail.Errors)
                    {
                        errors.Add(new ValidationError
                        {
                            Path = detail.InstanceLocation.ToString(),
                            Message = error.Value,
                            Code = error.Key
                        });
                    }
                }
            }
        }

        if (results.Details != null)
        {
            foreach (var detail in results.Details)
            {
                CollectErrors(detail, errors);
            }
        }
    }
}

public sealed class ValidationResult
{
    public required bool IsValid { get; init; }
    public ValidationError[]? Errors { get; init; }

    public static ValidationResult Success() => new() { IsValid = true, Errors = [] };

    public static ValidationResult Failure(params ValidationError[] errors) => new()
    {
        IsValid = false,
        Errors = errors
    };
}

public sealed class ValidationError
{
    public required string Path { get; init; }
    public required string Message { get; init; }
    public required string Code { get; init; }
    public string? ExpectedType { get; init; }
    public string? ActualType { get; init; }
}
