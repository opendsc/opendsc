// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Nodes;

using Json.Schema;

using OpenDsc.Contracts.Configurations;

using YamlDotNet.Serialization;

namespace OpenDsc.Server.Services;

public interface IParameterValidator
{
    /// <summary>
    /// Validates parameter file content against a JSON Schema.
    /// </summary>
    ValidationResult Validate(string jsonSchemaString, string parameterFileContent);
}

public sealed partial class ParameterValidator(ILogger<ParameterValidator> logger) : IParameterValidator
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

            // Parse parameter file (YAML or JSON).
            // Detect format by content prefix to avoid throwing JsonReaderException for YAML input.
            JsonNode? paramContent;
            var trimmedContent = parameterFileContent.TrimStart();
            if (trimmedContent.StartsWith('{') || trimmedContent.StartsWith('['))
            {
                try
                {
                    paramContent = JsonNode.Parse(parameterFileContent);
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
            else
            {
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

            LogParameterValidationFailed(errors.Count);
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

    [LoggerMessage(EventId = EventIds.ParameterValidationFailed, Level = LogLevel.Warning, Message = "Parameter validation failed with {ErrorCount} error(s)")]
    private partial void LogParameterValidationFailed(int errorCount);
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


