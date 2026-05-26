// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace OpenDsc.Server.Services;

public interface IJsonYamlConverter
{
    string ConvertJsonToYaml(string json);
    string ConvertYamlToJson(string yaml);
}

public sealed class JsonYamlConverter : IJsonYamlConverter
{
    public string ConvertJsonToYaml(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var obj = ConvertJsonElementToObject(root);

            if (obj == null)
            {
                return string.Empty;
            }

            var serializer = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .Build();

            return serializer.Serialize(obj);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static object? ConvertJsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => ConvertJsonElementToObject(p.Value)),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(ConvertJsonElementToObject)
                .ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => null
        };
    }

    public string ConvertYamlToJson(string yaml)
    {
        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(yaml));
            if (stream.Documents.Count == 0)
            {
                return string.Empty;
            }

            var jsonNode = ConvertYamlNodeToJsonNode(stream.Documents[0].RootNode);
            return jsonNode?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static JsonNode? ConvertYamlNodeToJsonNode(YamlNode node)
    {
        switch (node)
        {
            case YamlMappingNode mapping:
                var obj = new JsonObject();
                foreach (var entry in mapping.Children)
                {
                    var key = ((YamlScalarNode)entry.Key).Value ?? string.Empty;
                    obj[key] = ConvertYamlNodeToJsonNode(entry.Value);
                }
                return obj;
            case YamlSequenceNode sequence:
                var array = new JsonArray();
                foreach (var item in sequence.Children)
                {
                    array.Add(ConvertYamlNodeToJsonNode(item));
                }
                return array;
            case YamlScalarNode scalar:
                return ConvertYamlScalarToJsonNode(scalar);
            default:
                return null;
        }
    }

    private static JsonNode? ConvertYamlScalarToJsonNode(YamlScalarNode scalar)
    {
        var value = scalar.Value;
        if (scalar.Style is ScalarStyle.SingleQuoted or ScalarStyle.DoubleQuoted) return JsonValue.Create(value);
        if (value is null or "" or "~" or "null") return null;
        if (value is "true" or "True" or "TRUE") return JsonValue.Create(true);
        if (value is "false" or "False" or "FALSE") return JsonValue.Create(false);
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)) return JsonValue.Create(l);
        if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)) return JsonValue.Create(d);
        return JsonValue.Create(value);
    }
}
