// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Nodes;

using NuGet.Versioning;

using OpenDsc.Contracts.Configurations;

namespace OpenDsc.Server.Services;

public interface IParameterCompatibilityService
{
    /// <summary>
    /// Compares two parameter schemas and detects breaking/non-breaking changes.
    /// </summary>
    CompatibilityReport CompareSchemas(string? oldSchemaJson, string? newSchemaJson, string oldVersion, string newVersion);
}

public sealed partial class ParameterCompatibilityService(ILogger<ParameterCompatibilityService> logger) : IParameterCompatibilityService
{
    public CompatibilityReport CompareSchemas(string? oldSchemaJson, string? newSchemaJson, string oldVersion, string newVersion)
    {
        var breakingChanges = new List<SchemaChange>();
        var nonBreakingChanges = new List<SchemaChange>();

        // Parse versions
        if (!SemanticVersion.TryParse(oldVersion, out var oldSemVer) || !SemanticVersion.TryParse(newVersion, out var newSemVer))
        {
            throw new ArgumentException("Invalid semantic version format");
        }

        // Handle empty or null schemas
        if (string.IsNullOrWhiteSpace(oldSchemaJson) && string.IsNullOrWhiteSpace(newSchemaJson))
        {
            return new CompatibilityReport
            {
                OldVersion = oldVersion,
                NewVersion = newVersion,
                NewMajorVersion = newSemVer.Major,
                HasBreakingChanges = false,
                BreakingChanges = [],
                NonBreakingChanges = [],
                AffectedParameterFiles = []
            };
        }

        Dictionary<string, ParameterInfo>? oldParams = null;
        Dictionary<string, ParameterInfo>? newParams = null;

        if (!string.IsNullOrWhiteSpace(oldSchemaJson))
        {
            oldParams = ExtractParametersFromSchema(oldSchemaJson);
        }

        if (!string.IsNullOrWhiteSpace(newSchemaJson))
        {
            newParams = ExtractParametersFromSchema(newSchemaJson);
        }

        if (oldParams == null && newParams != null)
        {
            // All parameters are new additions
            foreach (var param in newParams)
            {
                var isRequired = param.Value.IsRequired;
                if (isRequired)
                {
                    breakingChanges.Add(new SchemaChange
                    {
                        ParameterName = param.Key,
                        ChangeType = "ParameterAdded",
                        NewValue = param.Value.Type,
                        Description = $"New required parameter '{param.Key}' added"
                    });
                }
                else
                {
                    nonBreakingChanges.Add(new SchemaChange
                    {
                        ParameterName = param.Key,
                        ChangeType = "ParameterAdded",
                        NewValue = param.Value.Type,
                        Description = $"New optional parameter '{param.Key}' added"
                    });
                }
            }
        }
        else if (oldParams != null && newParams == null)
        {
            // All parameters removed
            foreach (var param in oldParams)
            {
                breakingChanges.Add(new SchemaChange
                {
                    ParameterName = param.Key,
                    ChangeType = "ParameterRemoved",
                    OldValue = param.Value.Type,
                    Description = $"Parameter '{param.Key}' removed"
                });
            }
        }
        else if (oldParams != null && newParams != null)
        {
            // Compare parameters
            foreach (var oldParam in oldParams)
            {
                if (!newParams.TryGetValue(oldParam.Key, out var newParam))
                {
                    // Parameter removed
                    breakingChanges.Add(new SchemaChange
                    {
                        ParameterName = oldParam.Key,
                        ChangeType = "ParameterRemoved",
                        OldValue = oldParam.Value.Type,
                        Description = $"Parameter '{oldParam.Key}' removed"
                    });
                }
                else
                {
                    // Parameter exists in both - check for changes
                    CompareParameter(oldParam.Key, oldParam.Value, newParam, breakingChanges, nonBreakingChanges);
                }
            }

            // Check for new parameters
            foreach (var newParam in newParams)
            {
                if (!oldParams.ContainsKey(newParam.Key))
                {
                    var isRequired = newParam.Value.IsRequired;
                    if (isRequired)
                    {
                        breakingChanges.Add(new SchemaChange
                        {
                            ParameterName = newParam.Key,
                            ChangeType = "ParameterAdded",
                            NewValue = newParam.Value.Type,
                            Description = $"New required parameter '{newParam.Key}' added"
                        });
                    }
                    else
                    {
                        nonBreakingChanges.Add(new SchemaChange
                        {
                            ParameterName = newParam.Key,
                            ChangeType = "ParameterAdded",
                            NewValue = newParam.Value.Type,
                            Description = $"New optional parameter '{newParam.Key}' added"
                        });
                    }
                }
            }
        }

        if (breakingChanges.Count > 0)
        {
            LogCompatibilityBreakingChangesDetected(breakingChanges.Count, oldVersion, newVersion);
        }

        LogSchemaComparisonComplete(breakingChanges.Count, nonBreakingChanges.Count, oldVersion, newVersion);

        return new CompatibilityReport
        {
            OldVersion = oldVersion,
            NewVersion = newVersion,
            NewMajorVersion = newSemVer.Major,
            HasBreakingChanges = breakingChanges.Count > 0,
            BreakingChanges = breakingChanges,
            NonBreakingChanges = nonBreakingChanges,
            AffectedParameterFiles = []
        };
    }

    private static void CompareParameter(string paramName, ParameterInfo oldParam, ParameterInfo newParam,
        List<SchemaChange> breakingChanges, List<SchemaChange> nonBreakingChanges)
    {
        // Type change
        if (oldParam.Type != newParam.Type)
        {
            breakingChanges.Add(new SchemaChange
            {
                ParameterName = paramName,
                ChangeType = "TypeChanged",
                OldValue = oldParam.Type,
                NewValue = newParam.Type,
                Description = $"Parameter '{paramName}' type changed from {oldParam.Type} to {newParam.Type}"
            });
            return; // Other changes don't matter if type changed
        }

        // Required status change
        if (!oldParam.IsRequired && newParam.IsRequired)
        {
            breakingChanges.Add(new SchemaChange
            {
                ParameterName = paramName,
                ChangeType = "BecameRequired",
                Description = $"Parameter '{paramName}' is now required"
            });
        }
        else if (oldParam.IsRequired && !newParam.IsRequired)
        {
            nonBreakingChanges.Add(new SchemaChange
            {
                ParameterName = paramName,
                ChangeType = "BecameOptional",
                Description = $"Parameter '{paramName}' is now optional"
            });
        }

        // Enum/AllowedValues changes
        if (oldParam.AllowedValues != null && newParam.AllowedValues != null)
        {
            var oldValues = new HashSet<string>(oldParam.AllowedValues);
            var newValues = new HashSet<string>(newParam.AllowedValues);
            var removed = oldValues.Except(newValues).ToList();
            var added = newValues.Except(oldValues).ToList();

            if (removed.Count > 0)
            {
                breakingChanges.Add(new SchemaChange
                {
                    ParameterName = paramName,
                    ChangeType = "AllowedValuesReduced",
                    OldValue = string.Join(", ", removed),
                    Description = $"Parameter '{paramName}' no longer allows values: {string.Join(", ", removed)}"
                });
            }

            if (added.Count > 0)
            {
                nonBreakingChanges.Add(new SchemaChange
                {
                    ParameterName = paramName,
                    ChangeType = "AllowedValuesExpanded",
                    NewValue = string.Join(", ", added),
                    Description = $"Parameter '{paramName}' now allows additional values: {string.Join(", ", added)}"
                });
            }
        }
        else if (oldParam.AllowedValues != null && newParam.AllowedValues == null)
        {
            nonBreakingChanges.Add(new SchemaChange
            {
                ParameterName = paramName,
                ChangeType = "AllowedValuesRemoved",
                Description = $"Parameter '{paramName}' no longer has allowed values restriction"
            });
        }
        else if (oldParam.AllowedValues == null && newParam.AllowedValues != null)
        {
            breakingChanges.Add(new SchemaChange
            {
                ParameterName = paramName,
                ChangeType = "AllowedValuesAdded",
                NewValue = string.Join(", ", newParam.AllowedValues),
                Description = $"Parameter '{paramName}' now restricts values to: {string.Join(", ", newParam.AllowedValues)}"
            });
        }

        // Min/Max constraints for integers
        if (oldParam.Type == "integer")
        {
            if (oldParam.MinValue.HasValue && newParam.MinValue.HasValue && newParam.MinValue.Value > oldParam.MinValue.Value)
            {
                breakingChanges.Add(new SchemaChange
                {
                    ParameterName = paramName,
                    ChangeType = "MinValueIncreased",
                    OldValue = oldParam.MinValue.Value.ToString(),
                    NewValue = newParam.MinValue.Value.ToString(),
                    Description = $"Parameter '{paramName}' minimum value increased from {oldParam.MinValue.Value} to {newParam.MinValue.Value}"
                });
            }

            if (oldParam.MaxValue.HasValue && newParam.MaxValue.HasValue && newParam.MaxValue.Value < oldParam.MaxValue.Value)
            {
                breakingChanges.Add(new SchemaChange
                {
                    ParameterName = paramName,
                    ChangeType = "MaxValueDecreased",
                    OldValue = oldParam.MaxValue.Value.ToString(),
                    NewValue = newParam.MaxValue.Value.ToString(),
                    Description = $"Parameter '{paramName}' maximum value decreased from {oldParam.MaxValue.Value} to {newParam.MaxValue.Value}"
                });
            }
        }

        // Min/Max length constraints
        if (oldParam.Type is "string" or "array")
        {
            if (oldParam.MinLength.HasValue && newParam.MinLength.HasValue && newParam.MinLength.Value > oldParam.MinLength.Value)
            {
                breakingChanges.Add(new SchemaChange
                {
                    ParameterName = paramName,
                    ChangeType = "MinLengthIncreased",
                    OldValue = oldParam.MinLength.Value.ToString(),
                    NewValue = newParam.MinLength.Value.ToString(),
                    Description = $"Parameter '{paramName}' minimum length increased from {oldParam.MinLength.Value} to {newParam.MinLength.Value}"
                });
            }

            if (oldParam.MaxLength.HasValue && newParam.MaxLength.HasValue && newParam.MaxLength.Value < oldParam.MaxLength.Value)
            {
                breakingChanges.Add(new SchemaChange
                {
                    ParameterName = paramName,
                    ChangeType = "MaxLengthDecreased",
                    OldValue = oldParam.MaxLength.Value.ToString(),
                    NewValue = newParam.MaxLength.Value.ToString(),
                    Description = $"Parameter '{paramName}' maximum length decreased from {oldParam.MaxLength.Value} to {newParam.MaxLength.Value}"
                });
            }
        }
    }

    private static Dictionary<string, ParameterInfo> ExtractParametersFromSchema(string schemaJson)
    {
        var result = new Dictionary<string, ParameterInfo>();

        var schemaNode = JsonNode.Parse(schemaJson);
        if (schemaNode == null)
        {
            return result;
        }

        // Schema has structure: { properties: { parameters: { properties: { param1: {...}, ... }, required: [...] } } }
        // Extract the "parameters" object first
        var rootPropertiesNode = schemaNode["properties"];
        if (rootPropertiesNode == null)
        {
            return result;
        }

        var parametersNode = rootPropertiesNode["parameters"];
        if (parametersNode == null)
        {
            return result;
        }

        var propertiesNode = parametersNode["properties"];
        if (propertiesNode == null)
        {
            return result;
        }

        var requiredNode = parametersNode["required"];
        var requiredParams = new HashSet<string>();
        if (requiredNode is JsonArray requiredArray)
        {
            foreach (var item in requiredArray)
            {
                if (item != null)
                {
                    requiredParams.Add(item.GetValue<string>());
                }
            }
        }

        foreach (var prop in propertiesNode.AsObject())
        {
            var paramName = prop.Key;
            var paramSchema = prop.Value;

            if (paramSchema == null)
            {
                continue;
            }

            var paramInfo = new ParameterInfo
            {
                IsRequired = requiredParams.Contains(paramName)
            };

            // Extract type
            var typeNode = paramSchema["type"];
            if (typeNode != null)
            {
                paramInfo.Type = typeNode.GetValue<string>();
            }

            // Extract enum/allowedValues
            var enumNode = paramSchema["enum"];
            if (enumNode is JsonArray enumArray)
            {
                paramInfo.AllowedValues = enumArray.Select(e => e?.ToString() ?? "").ToArray();
            }

            // Extract min/max constraints
            var minNode = paramSchema["minimum"];
            if (minNode != null && int.TryParse(minNode.ToString(), out var minVal))
            {
                paramInfo.MinValue = minVal;
            }

            var maxNode = paramSchema["maximum"];
            if (maxNode != null && int.TryParse(maxNode.ToString(), out var maxVal))
            {
                paramInfo.MaxValue = maxVal;
            }

            var minLenNode = paramSchema["minLength"];
            if (minLenNode != null && int.TryParse(minLenNode.ToString(), out var minLen))
            {
                paramInfo.MinLength = minLen;
            }

            var maxLenNode = paramSchema["maxLength"];
            if (maxLenNode != null && int.TryParse(maxLenNode.ToString(), out var maxLen))
            {
                paramInfo.MaxLength = maxLen;
            }

            result[paramName] = paramInfo;
        }

        return result;
    }

    private sealed class ParameterInfo
    {
        public string Type { get; set; } = "string";
        public bool IsRequired { get; set; }
        public string[]? AllowedValues { get; set; }
        public int? MinValue { get; set; }
        public int? MaxValue { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
    }

    [LoggerMessage(EventId = EventIds.CompatibilityBreakingChangesDetected, Level = LogLevel.Warning, Message = "{BreakingCount} breaking change(s) detected between schema versions {OldVersion} and {NewVersion}")]
    private partial void LogCompatibilityBreakingChangesDetected(int breakingCount, string oldVersion, string newVersion);

    [LoggerMessage(EventId = EventIds.SchemaComparisonComplete, Level = LogLevel.Debug, Message = "Schema comparison complete: {BreakingCount} breaking, {NonBreakingCount} non-breaking changes between {OldVersion} and {NewVersion}")]
    private partial void LogSchemaComparisonComplete(int breakingCount, int nonBreakingCount, string oldVersion, string newVersion);
}


