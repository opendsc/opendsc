// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
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
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var yamlObj = deserializer.Deserialize<Dictionary<object, object>>(yaml);
            if (yamlObj == null)
            {
                return string.Empty;
            }

            var converted = ConvertYamlObject(yamlObj);
            return JsonSerializer.Serialize(converted, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return string.Empty;
        }
    }

    private static object? ConvertYamlObject(object? obj)
    {
        return obj switch
        {
            Dictionary<object, object> dict => dict.ToDictionary(
                kvp => kvp.Key?.ToString() ?? string.Empty,
                kvp => ConvertYamlObject(kvp.Value)),
            List<object> list => list.Select(ConvertYamlObject).ToList(),
            _ => obj
        };
    }
}
