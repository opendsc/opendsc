// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace OpenDsc.Server.Services;

internal interface IParameterSource
{
    string ScopeTypeName { get; }
    string? ScopeValue { get; }
    int Precedence { get; }
}

/// <summary>
/// Implements parameter merging with deep recursive merge for objects and replace for arrays.
/// </summary>
public sealed class ParameterMerger : IParameterMerger
{
    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    public string Merge(IEnumerable<string> parameterFiles, MergeOptions? options = null)
    {
        options ??= new MergeOptions();

        var parsedParameters = parameterFiles
            .Select(ParseParameterFile)
            .ToList();

        var merged = MergeObjects(parsedParameters);

        return options.OutputFormat == ParameterFormat.Yaml
            ? SerializeToYaml(merged)
            : SerializeToJson(merged);
    }

    public MergeResult MergeWithProvenance(IEnumerable<ParameterSource> parameterFiles, MergeOptions? options = null)
    {
        options ??= new MergeOptions();

        var sources = parameterFiles.OrderBy(p => p.Precedence).ToList();
        var parsedParameters = sources.Select(s => new ParameterSourceInternal
        {
            ScopeTypeName = s.ScopeTypeName,
            ScopeValue = s.ScopeValue,
            Precedence = s.Precedence,
            Parameters = ParseParameterFile(s.Content)
        }).ToList();

        var provenance = new Dictionary<string, ParameterProvenance>();
        var merged = MergeObjectsWithProvenance(parsedParameters.Select(p => p.Parameters).ToList(), parsedParameters, string.Empty, provenance);

        var mergedContent = options.OutputFormat == ParameterFormat.Yaml
            ? SerializeToYaml(merged)
            : SerializeToJson(merged);

        return new MergeResult
        {
            MergedContent = mergedContent,
            Provenance = provenance
        };
    }

    private sealed class ParameterSourceInternal : IParameterSource
    {
        public string ScopeTypeName { get; set; } = string.Empty;
        public string? ScopeValue { get; set; }
        public int Precedence { get; set; }
        public Dictionary<string, object?> Parameters { get; set; } = new();
    }

    private static Dictionary<string, object?> ParseParameterFile(string content)
    {
        content = content.Trim();

        if (content.StartsWith("{", StringComparison.Ordinal))
        {
            var jsonDoc = JsonSerializer.Deserialize<JsonElement>(content);
            return ConvertJsonElement(jsonDoc);
        }

        var yamlObj = YamlDeserializer.Deserialize(content);
        return ConvertToDict(yamlObj) ?? [];
    }

    private static Dictionary<string, object?> ConvertJsonElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var property in element.EnumerateObject())
            {
                dict[property.Name] = ConvertJsonValue(property.Value);
            }
            return dict;
        }

        return [];
    }

    private static object? ConvertJsonValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertJsonElement(element),
            JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt32(out var intVal) ? intVal :
                                    element.TryGetInt64(out var longVal) ? longVal :
                                    (object?)element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    private static Dictionary<string, object?> ConvertToDict(object? obj)
    {
        if (obj is Dictionary<object, object> dict)
        {
            return dict.ToDictionary(
                kvp => kvp.Key?.ToString() ?? string.Empty,
                kvp => ConvertValue(kvp.Value)
            );
        }

        return obj as Dictionary<string, object?> ?? [];
    }

    private static object? ConvertValue(object? value)
    {
        return value switch
        {
            Dictionary<object, object> dict => ConvertToDict(dict),
            List<object> list => list.Select(ConvertValue).ToList(),
            _ => value
        };
    }

    private static Dictionary<string, object?> MergeObjects(List<Dictionary<string, object?>> objects)
    {
        if (objects.Count == 0)
        {
            return [];
        }

        var result = new Dictionary<string, object?>(objects[0]);

        for (int i = 1; i < objects.Count; i++)
        {
            MergeInto(result, objects[i]);
        }

        return result;
    }

    private static Dictionary<string, object?> MergeObjectsWithProvenance<T>(
        List<Dictionary<string, object?>> objects,
        List<T> sources,
        string path,
        Dictionary<string, ParameterProvenance> provenance) where T : IParameterSource
    {
        if (objects.Count == 0)
        {
            return [];
        }

        var result = new Dictionary<string, object?>(objects[0]);

        for (int i = 1; i < objects.Count; i++)
        {
            MergeIntoWithProvenance(result, objects[i], sources, i, path, provenance);
        }

        return result;
    }

    private static void MergeInto(Dictionary<string, object?> target, Dictionary<string, object?> source)
    {
        foreach (var kvp in source)
        {
            if (!target.TryGetValue(kvp.Key, out var existingValue))
            {
                target[kvp.Key] = kvp.Value;
                continue;
            }

            if (kvp.Value is Dictionary<string, object?> sourceDict &&
                existingValue is Dictionary<string, object?> targetDict)
            {
                MergeInto(targetDict, sourceDict);
            }
            else
            {
                target[kvp.Key] = kvp.Value;
            }
        }
    }

    private static void MergeIntoWithProvenance<T>(
        Dictionary<string, object?> target,
        Dictionary<string, object?> source,
        List<T> sources,
        int currentIndex,
        string parentPath,
        Dictionary<string, ParameterProvenance> provenance) where T : IParameterSource
    {
        foreach (var kvp in source)
        {
            var currentPath = string.IsNullOrEmpty(parentPath) ? kvp.Key : $"{parentPath}.{kvp.Key}";

            if (!target.TryGetValue(kvp.Key, out var existingValue))
            {
                target[kvp.Key] = kvp.Value;
                provenance[currentPath] = new ParameterProvenance
                {
                    ScopeTypeName = sources[currentIndex].ScopeTypeName,
                    ScopeValue = sources[currentIndex].ScopeValue,
                    Precedence = sources[currentIndex].Precedence,
                    Value = kvp.Value
                };
                continue;
            }

            if (kvp.Value is Dictionary<string, object?> sourceDict &&
                existingValue is Dictionary<string, object?> targetDict)
            {
                MergeIntoWithProvenance(targetDict, sourceDict, sources, currentIndex, currentPath, provenance);
            }
            else
            {
                var overriddenValues = new List<ScopeValueInfo>();

                if (provenance.TryGetValue(currentPath, out var existing))
                {
                    overriddenValues.Add(new ScopeValueInfo
                    {
                        ScopeTypeName = existing.ScopeTypeName,
                        ScopeValue = existing.ScopeValue,
                        Precedence = existing.Precedence,
                        Value = existing.Value
                    });

                    if (existing.OverriddenValues != null)
                    {
                        overriddenValues.AddRange(existing.OverriddenValues);
                    }
                }

                target[kvp.Key] = kvp.Value;
                provenance[currentPath] = new ParameterProvenance
                {
                    ScopeTypeName = sources[currentIndex].ScopeTypeName,
                    ScopeValue = sources[currentIndex].ScopeValue,
                    Precedence = sources[currentIndex].Precedence,
                    Value = kvp.Value,
                    OverriddenValues = overriddenValues.Count > 0 ? overriddenValues : null
                };
            }
        }
    }

    private static string SerializeToYaml(Dictionary<string, object?> obj)
    {
        return YamlSerializer.Serialize(obj);
    }

    private static string SerializeToJson(Dictionary<string, object?> obj)
    {
        return JsonSerializer.Serialize(obj, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
