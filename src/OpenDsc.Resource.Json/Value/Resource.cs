// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using global::Json.Path;
using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Json.Value;

[DscResource("OpenDsc.Json/Value", "0.1.0", Description = "Manage JSON values at JSONPath locations", Tags = ["json", "value", "jsonpath"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(FileNotFoundException), Description = "JSON file not found")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[ExitCode(5, Exception = typeof(IOException), Description = "IO error")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>
{
    public override string GetSchema()
    {
        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase
        };

        var builder = new JsonSchemaBuilder().FromType<Schema>(config);
        builder.Schema("https://json-schema.org/draft/2020-12/schema");
        var schema = builder.Build();

        // Override the 'value' property schema to be permissive (accept any JSON type)
        // JsonSchema.Net.Generation treats JsonElement as an object with properties,
        // but we want it to accept any valid JSON value (string, number, boolean, null, object, array)
        var schemaObj = JsonNode.Parse(JsonSerializer.Serialize(schema))?.AsObject();
        if (schemaObj?["properties"]?["value"] is JsonObject valueSchema)
        {
            // Remove type constraint to allow any JSON value
            valueSchema.Remove("type");
            valueSchema.Remove("properties");
        }

        return schemaObj?.ToJsonString() ?? JsonSerializer.Serialize(schema);
    }

    public Schema Get(Schema instance)
    {
        if (!File.Exists(instance.Path))
        {
            return new Schema()
            {
                Path = instance.Path,
                JsonPath = instance.JsonPath,
                Exist = false
            };
        }

        var content = File.ReadAllText(instance.Path);
        var doc = JsonNode.Parse(content);

        if (doc == null)
        {
            throw new JsonException($"Failed to parse JSON document: {instance.Path}");
        }

        var path = JsonPath.Parse(instance.JsonPath);
        var results = path.Evaluate(doc);

        if (!results.Matches.Any())
        {
            return new Schema()
            {
                Path = instance.Path,
                JsonPath = instance.JsonPath,
                Exist = false
            };
        }

        var firstMatch = results.Matches.First();
        var valueJson = firstMatch.Value?.ToJsonString() ?? "null";
        var jsonElement = JsonSerializer.Deserialize<JsonElement>(valueJson);

        return new Schema()
        {
            Path = instance.Path,
            JsonPath = instance.JsonPath,
            Value = jsonElement
        };
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        if (!File.Exists(instance.Path))
        {
            throw new FileNotFoundException($"JSON file not found: {instance.Path}");
        }

        var content = File.ReadAllText(instance.Path);
        var doc = JsonNode.Parse(content);

        if (doc == null)
        {
            throw new JsonException($"Failed to parse JSON document: {instance.Path}");
        }

        // When System.Text.Json deserializes {"value": null}, it sets JsonElement? to C# null
        // So we treat C# null as "create JSON null value"
        JsonNode valueNode;
        if (!instance.Value.HasValue || instance.Value.Value.ValueKind == JsonValueKind.Null)
        {
            valueNode = JsonValue.Create((object?)null)!;
        }
        else
        {
            valueNode = JsonSerializer.Deserialize<JsonNode>(instance.Value.Value)
                ?? throw new JsonException("Failed to deserialize JSON value");
        }

        var targetNode = FindOrCreatePath(doc, instance.JsonPath);
        ReplaceNodeValue(targetNode.Parent, targetNode.PropertyName, targetNode.Index, valueNode);

        var writeIndented = IsIndented(content);
        var options = new JsonSerializerOptions { WriteIndented = writeIndented };
        File.WriteAllText(instance.Path, doc.ToJsonString(options), Encoding.UTF8);
        return null;
    }

    public void Delete(Schema instance)
    {
        if (!File.Exists(instance.Path))
        {
            return;
        }

        var content = File.ReadAllText(instance.Path);
        var doc = JsonNode.Parse(content);

        if (doc == null)
        {
            return;
        }

        var path = JsonPath.Parse(instance.JsonPath);
        var results = path.Evaluate(doc);

        if (results.Matches.Any())
        {
            var firstMatch = results.Matches.First();
            var value = firstMatch.Value;
            var parent = value?.Parent;

            if (parent is JsonObject obj && firstMatch.Location != null)
            {
                var locationPath = firstMatch.Location.ToString();
                var propertyName = ExtractPropertyName(locationPath);
                if (propertyName != null)
                {
                    obj.Remove(propertyName);
                }
            }
            else if (parent is JsonArray arr && value != null)
            {
                arr.Remove(value);
            }

            var writeIndented = IsIndented(content);
            var options = new JsonSerializerOptions { WriteIndented = writeIndented };
            File.WriteAllText(instance.Path, doc.ToJsonString(options), Encoding.UTF8);
        }
    }

    private static (JsonNode? Parent, string? PropertyName, int? Index) FindOrCreatePath(JsonNode doc, string jsonPath)
    {
        var path = JsonPath.Parse(jsonPath);
        var results = path.Evaluate(doc);

        if (results.Matches.Any())
        {
            var firstMatch = results.Matches.First();
            var existingValue = firstMatch.Value;
            var existingParent = existingValue?.Parent;

            if (existingParent is JsonObject && firstMatch.Location != null)
            {
                var locationPath = firstMatch.Location.ToString();
                var propertyName = ExtractPropertyName(locationPath);
                return (existingParent, propertyName, null);
            }
            else if (existingParent is JsonArray && firstMatch.Location != null)
            {
                var locationPath = firstMatch.Location.ToString();
                var index = ExtractArrayIndex(locationPath);
                if (index.HasValue)
                {
                    return (existingParent, null, index);
                }
            }
        }

        var segments = ParseJsonPath(jsonPath);
        JsonNode current = doc;
        JsonNode? parent = null;
        string? lastPropertyName = null;
        int? lastIndex = null;

        for (int i = 1; i < segments.Count; i++)
        {
            var segment = segments[i];
            parent = current;

            if (segment.IsArray)
            {
                if (current is not JsonArray currentArray)
                {
                    var newArray = new JsonArray();
                    if (parent is JsonObject parentObj && lastPropertyName != null)
                    {
                        parentObj[lastPropertyName] = newArray;
                    }
                    currentArray = newArray;
                    current = currentArray;
                }

                while (currentArray.Count <= segment.Index)
                {
                    currentArray.Add(JsonValue.Create<object?>(null));
                }

                if (i == segments.Count - 1)
                {
                    lastIndex = segment.Index;
                    return (current, null, lastIndex);
                }

                var item = currentArray[segment.Index];
                if (item == null)
                {
                    var nextSegment = segments[i + 1];
                    item = nextSegment.IsArray ? new JsonArray() : new JsonObject();
                    currentArray[segment.Index] = item;
                }
                current = item;
                lastIndex = segment.Index;
            }
            else
            {
                if (current is not JsonObject currentObj)
                {
                    var newObj = new JsonObject();
                    if (parent is JsonObject parentObj && lastPropertyName != null)
                    {
                        parentObj[lastPropertyName] = newObj;
                    }
                    else if (parent is JsonArray parentArr && lastIndex.HasValue)
                    {
                        parentArr[lastIndex.Value] = newObj;
                    }
                    currentObj = newObj;
                    current = currentObj;
                }

                if (i == segments.Count - 1)
                {
                    lastPropertyName = segment.Name;
                    return (current, lastPropertyName, null);
                }

                if (!currentObj.ContainsKey(segment.Name))
                {
                    var nextSegment = segments[i + 1];
                    currentObj[segment.Name] = nextSegment.IsArray ? new JsonArray() : new JsonObject();
                }

                lastPropertyName = segment.Name;
                current = currentObj[segment.Name]!;
            }
        }

        return (parent, lastPropertyName, lastIndex);
    }

    private static void ReplaceNodeValue(JsonNode? parent, string? propertyName, int? index, JsonNode newValue)
    {
        if (parent is JsonObject obj && propertyName != null)
        {
            obj[propertyName] = newValue;
        }
        else if (parent is JsonArray arr && index.HasValue)
        {
            arr[index.Value] = newValue;
        }
    }

    private static string? ExtractPropertyName(string locationPath)
    {
        var match = System.Text.RegularExpressions.Regex.Match(locationPath, @"\['([^']+)'\]$");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return null;
    }

    private static int? ExtractArrayIndex(string locationPath)
    {
        var indexMatch = System.Text.RegularExpressions.Regex.Match(locationPath, @"\[(\d+)\]$");
        if (indexMatch.Success && int.TryParse(indexMatch.Groups[1].Value, out var index))
        {
            return index;
        }
        return null;
    }

    private static List<PathSegment> ParseJsonPath(string jsonPath)
    {
        var segments = new List<PathSegment> { new PathSegment { Name = "$", IsArray = false, Index = 0 } };

        var remaining = jsonPath.Substring(1);
        while (remaining.Length > 0)
        {
            if (remaining.StartsWith('.'))
            {
                remaining = remaining.Substring(1);
                var endIndex = remaining.IndexOfAny(['.', '[']);
                if (endIndex == -1) endIndex = remaining.Length;

                var propertyName = remaining.Substring(0, endIndex);
                segments.Add(new PathSegment { Name = propertyName, IsArray = false, Index = 0 });
                remaining = remaining.Substring(endIndex);
            }
            else if (remaining.StartsWith('['))
            {
                var closeBracket = remaining.IndexOf(']');
                if (closeBracket == -1)
                    throw new ArgumentException($"Invalid JSONPath: unclosed bracket in {jsonPath}");

                var indexStr = remaining.Substring(1, closeBracket - 1);

                if (indexStr.StartsWith('\'') || indexStr.StartsWith('"'))
                {
                    var propertyName = indexStr.Trim('\'', '"');
                    segments.Add(new PathSegment { Name = propertyName, IsArray = false, Index = 0 });
                }
                else if (int.TryParse(indexStr, out var index))
                {
                    segments.Add(new PathSegment { Name = string.Empty, IsArray = true, Index = index });
                }
                else
                {
                    throw new ArgumentException($"Invalid JSONPath: invalid array index or property name in {jsonPath}");
                }

                remaining = remaining.Substring(closeBracket + 1);
            }
            else
            {
                throw new ArgumentException($"Invalid JSONPath syntax: {jsonPath}");
            }
        }

        return segments;
    }

    private record PathSegment
    {
        public required string Name { get; init; }
        public required bool IsArray { get; init; }
        public required int Index { get; init; }
    }

    private static bool IsIndented(string jsonContent)
    {
        // Check if the JSON contains newlines (indented) or is minified
        // Trim end to ignore trailing newline that some editors add
        var trimmed = jsonContent.TrimEnd();
        return trimmed.Contains('\n') || trimmed.Contains('\r');
    }
}
