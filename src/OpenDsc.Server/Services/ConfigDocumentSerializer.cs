// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text;
using System.Text.Json.Nodes;

using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace OpenDsc.Server.Services;

public interface IConfigDocumentSerializer
{
    string Serialize(ConfigDocumentState state);
    ConfigDocumentState? Deserialize(string yaml);
}

public sealed class ConfigDocumentSerializer : IConfigDocumentSerializer
{
    public string Serialize(ConfigDocumentState state)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"$schema: {state.Schema}");

        if (state.Parameters.Count > 0)
        {
            sb.AppendLine("parameters:");
            foreach (var (name, param) in state.Parameters)
            {
                sb.AppendLine($"  {EscapeKey(name)}:");
                sb.AppendLine($"    type: {param.Type}");
                if (!string.IsNullOrWhiteSpace(param.Description))
                {
                    sb.AppendLine($"    description: {QuoteYamlString(param.Description)}");
                }
                if (param.DefaultValue is not null)
                {
                    if (IsScalarYamlValue(param.DefaultValue))
                    {
                        sb.AppendLine($"    defaultValue: {SerializeScalar(param.DefaultValue)}");
                    }
                    else
                    {
                        sb.AppendLine("    defaultValue:");
                        WriteComplexYamlValue(sb, ConvertRawValueToSerializable(param.DefaultValue), indent: 6);
                    }
                }
                if (param.AllowedValues?.Count > 0)
                {
                    sb.AppendLine("    allowedValues:");
                    foreach (var v in param.AllowedValues)
                    {
                        sb.AppendLine($"      - {SerializeScalar(v)}");
                    }
                }
                if (param.MinLength.HasValue) sb.AppendLine($"    minLength: {param.MinLength}");
                if (param.MaxLength.HasValue) sb.AppendLine($"    maxLength: {param.MaxLength}");
                if (param.MinValue.HasValue) sb.AppendLine($"    minValue: {param.MinValue}");
                if (param.MaxValue.HasValue) sb.AppendLine($"    maxValue: {param.MaxValue}");
            }
        }

        if (state.Variables.Count > 0)
        {
            sb.AppendLine("variables:");
            foreach (var (name, value) in state.Variables)
            {
                sb.AppendLine($"  {EscapeKey(name)}: {SerializeScalar(value)}");
            }
        }

        sb.AppendLine("resources:");
        foreach (var resource in state.Resources)
        {
            SerializeResource(sb, resource, indent: 2);
        }

        return sb.ToString();
    }

    private static void SerializeResource(StringBuilder sb, ConfigResourceState resource, int indent)
    {
        var pad = new string(' ', indent);
        sb.AppendLine($"{pad}- name: {QuoteYamlString(resource.Name)}");
        sb.AppendLine($"{pad}  type: {resource.Type}");
        if (!string.IsNullOrWhiteSpace(resource.Version))
        {
            sb.AppendLine($"{pad}  requireVersion: {resource.Version}");
        }

        if (resource.Condition is not null)
        {
            if (resource.Condition.IsFunction && resource.Condition.Function is not null)
            {
                sb.AppendLine($"{pad}  condition: {QuoteYamlString(resource.Condition.Function.ToInlineString())}");
            }
            else
            {
                sb.AppendLine($"{pad}  condition: {SerializeScalar(resource.Condition.RawValue)}");
            }
        }

        if (resource.DependsOn.Count > 0)
        {
            sb.AppendLine($"{pad}  dependsOn:");
            foreach (var dep in resource.DependsOn)
            {
                sb.AppendLine($"{pad}    - {QuoteYamlString(dep)}");
            }
        }

        if (resource.Properties.Count > 0 || resource.NestedResources.Count > 0)
        {
            sb.AppendLine($"{pad}  properties:");
            foreach (var (propName, propValue) in resource.Properties)
            {
                if (propValue.IsFunction && propValue.Function is not null)
                {
                    sb.AppendLine($"{pad}    {EscapeKey(propName)}: {QuoteYamlString(propValue.Function.ToInlineString())}");
                    continue;
                }

                var serializedRaw = ConvertRawValueToSerializable(propValue.RawValue);
                if (IsScalarYamlValue(serializedRaw))
                {
                    sb.AppendLine($"{pad}    {EscapeKey(propName)}: {SerializeScalar(serializedRaw)}");
                }
                else
                {
                    sb.AppendLine($"{pad}    {EscapeKey(propName)}:");
                    WriteComplexYamlValue(sb, serializedRaw, indent + 6);
                }
            }

            if (resource.NestedResources.Count > 0)
            {
                sb.AppendLine($"{pad}    resources:");
                foreach (var nested in resource.NestedResources)
                {
                    SerializeResource(sb, nested, indent + 6);
                }
            }
        }
    }

    private static bool IsScalarYamlValue(object? value)
    {
        return value is null or bool or string or sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }

    private static object? ConvertRawValueToSerializable(object? value)
    {
        return value switch
        {
            JsonNode node => ConvertJsonNodeToObject(node),
            IDictionary<string, object?> dict => dict.ToDictionary(kvp => kvp.Key, kvp => ConvertRawValueToSerializable(kvp.Value), StringComparer.Ordinal),
            IEnumerable<object?> seq => seq.Select(ConvertRawValueToSerializable).ToList(),
            _ => value
        };
    }

    private static object? ConvertJsonNodeToObject(JsonNode? node)
    {
        return node switch
        {
            null => null,
            JsonObject obj => obj.ToDictionary(kvp => kvp.Key, kvp => ConvertJsonNodeToObject(kvp.Value), StringComparer.Ordinal),
            JsonArray arr => arr.Select(ConvertJsonNodeToObject).ToList(),
            JsonValue value => value.TryGetValue<object>(out var raw) ? raw : value.ToJsonString(),
            _ => node.ToJsonString()
        };
    }

    private static void WriteComplexYamlValue(StringBuilder sb, object? value, int indent)
    {
        var serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        var yaml = serializer.Serialize(value ?? new object()).TrimEnd('\r', '\n');
        var pad = new string(' ', indent);
        foreach (var line in yaml.Split('\n'))
        {
            sb.AppendLine($"{pad}{line}");
        }
    }

    private static string SerializeScalar(object? value)
    {
        return value switch
        {
            null => "null",
            bool b => b ? "true" : "false",
            string s => QuoteYamlString(s),
            _ => value.ToString() ?? "null"
        };
    }

    private static string QuoteYamlString(string s)
    {
        // Use double-quote if the string contains special chars or YAML reserved words
        if (string.IsNullOrEmpty(s)) return "''";

        var needsQuoting = s.Contains(':') || s.Contains('#') || s.Contains('[') ||
                           s.Contains(']') || s.Contains('{') || s.Contains('}') ||
                           s.Contains('\'') || s.StartsWith('"') || s.StartsWith(' ') ||
                           s.EndsWith(' ') || s == "null" || s == "true" || s == "false" ||
                           s.StartsWith('|') || s.StartsWith('>');

        if (!needsQuoting && !s.Contains('"'))
        {
            return s;
        }

        return $"\"{s.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }

    private static string EscapeKey(string key)
    {
        return key.Contains(':') || key.Contains(' ') || key.Contains('\'')
            ? $"\"{key.Replace("\"", "\\\"")}\""
            : key;
    }

    // -------------------------------------------------------------------------
    // Deserialization
    // -------------------------------------------------------------------------

    public ConfigDocumentState? Deserialize(string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml)) return null;

        var stream = new YamlStream();
        try
        {
            stream.Load(new StringReader(yaml));
        }
        catch
        {
            return null;
        }

        if (stream.Documents.Count == 0) return null;

        var root = stream.Documents[0].RootNode as YamlMappingNode;
        if (root is null) return null;

        var state = new ConfigDocumentState();

        if (TryGetString(root, "$schema") is { } schema)
        {
            state.Schema = schema;
        }

        if (root.Children.TryGetValue(new YamlScalarNode("parameters"), out var paramsNode) &&
            paramsNode is YamlMappingNode paramsMap)
        {
            foreach (var (keyNode, valueNode) in paramsMap.Children)
            {
                if (keyNode is not YamlScalarNode keyScalar || valueNode is not YamlMappingNode paramMap)
                {
                    continue;
                }

                var paramName = keyScalar.Value ?? string.Empty;
                var param = new ConfigParameterState
                {
                    Type = TryGetString(paramMap, "type") ?? "string",
                    Description = TryGetString(paramMap, "description"),
                };

                if (paramMap.Children.TryGetValue(new YamlScalarNode("defaultValue"), out var defNode))
                {
                    param.DefaultValue = ParseYamlNodeValue(defNode);
                }

                if (paramMap.Children.TryGetValue(new YamlScalarNode("allowedValues"), out var avNode) &&
                    avNode is YamlSequenceNode avSeq)
                {
                    param.AllowedValues = avSeq.Children.Select(ParseScalarValue).OfType<object>().ToList();
                }

                if (TryGetString(paramMap, "minLength") is { } minLen && int.TryParse(minLen, out var minLenVal))
                    param.MinLength = minLenVal;
                if (TryGetString(paramMap, "maxLength") is { } maxLen && int.TryParse(maxLen, out var maxLenVal))
                    param.MaxLength = maxLenVal;
                if (TryGetString(paramMap, "minValue") is { } minVal && int.TryParse(minVal, out var minValVal))
                    param.MinValue = minValVal;
                if (TryGetString(paramMap, "maxValue") is { } maxVal && int.TryParse(maxVal, out var maxValVal))
                    param.MaxValue = maxValVal;

                state.Parameters[paramName] = param;
            }
        }

        if (root.Children.TryGetValue(new YamlScalarNode("variables"), out var varsNode) &&
            varsNode is YamlMappingNode varsMap)
        {
            foreach (var (keyNode, valueNode) in varsMap.Children)
            {
                if (keyNode is YamlScalarNode keyScalar)
                {
                    state.Variables[keyScalar.Value ?? ""] = ParseScalarValue(valueNode);
                }
            }
        }

        if (root.Children.TryGetValue(new YamlScalarNode("resources"), out var resourcesNode) &&
            resourcesNode is YamlSequenceNode resourcesSeq)
        {
            foreach (var resourceNode in resourcesSeq.Children)
            {
                if (resourceNode is YamlMappingNode resourceMap)
                {
                    var r = ParseResourceNode(resourceMap);
                    if (r is not null)
                    {
                        state.Resources.Add(r);
                    }
                }
            }
        }

        return state;
    }

    private static ConfigResourceState? ParseResourceNode(YamlMappingNode resourceMap)
    {
        var name = TryGetString(resourceMap, "name") ?? string.Empty;
        var type = TryGetString(resourceMap, "type") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(type)) return null;

        var resource = new ConfigResourceState
        {
            Name = name,
            Type = type,
            Version = TryGetString(resourceMap, "requireVersion") ?? TryGetString(resourceMap, "version")
        };

        if (resourceMap.Children.TryGetValue(new YamlScalarNode("condition"), out var conditionNode))
        {
            resource.Condition = ParsePropertyValue(conditionNode);
        }

        if (resourceMap.Children.TryGetValue(new YamlScalarNode("dependsOn"), out var depsNode) &&
            depsNode is YamlSequenceNode depsSeq)
        {
            resource.DependsOn = depsSeq.Children
                .OfType<YamlScalarNode>()
                .Select(n => n.Value ?? string.Empty)
                .ToList();
        }

        if (resourceMap.Children.TryGetValue(new YamlScalarNode("properties"), out var propsNode) &&
            propsNode is YamlMappingNode propsMap)
        {
            foreach (var (keyNode, valueNode) in propsMap.Children)
            {
                if (keyNode is not YamlScalarNode keyScalar) continue;
                var propName = keyScalar.Value ?? string.Empty;

                if (string.Equals(propName, "resources", StringComparison.Ordinal) &&
                    valueNode is YamlSequenceNode nestedSeq)
                {
                    foreach (var nestedNode in nestedSeq.Children)
                    {
                        if (nestedNode is YamlMappingNode nestedMap)
                        {
                            var nested = ParseResourceNode(nestedMap);
                            if (nested is not null) resource.NestedResources.Add(nested);
                        }
                    }
                    continue;
                }

                resource.Properties[propName] = ParsePropertyValue(valueNode);
            }
        }

        return resource;
    }

    private static ConfigPropertyValue ParsePropertyValue(YamlNode node)
    {
        if (node is YamlScalarNode scalar)
        {
            var str = scalar.Value ?? string.Empty;
            if (TryParseInlineFunctionExpression(str, out var expression))
            {
                return ConfigPropertyValue.FromFunction(expression);
            }

            return ConfigPropertyValue.Literal(ParseScalarValue(node));
        }

        return ConfigPropertyValue.Literal(ParseYamlNodeValue(node));
    }

    private static object? ParseYamlNodeValue(YamlNode node)
    {
        return node switch
        {
            YamlScalarNode scalar => ParseScalarValue(scalar),
            YamlSequenceNode sequence => sequence.Children.Select(ParseYamlNodeValue).ToList(),
            YamlMappingNode mapping => mapping.Children
                .Where(entry => entry.Key is YamlScalarNode)
                .ToDictionary(
                    entry => ((YamlScalarNode)entry.Key).Value ?? string.Empty,
                    entry => ParseYamlNodeValue(entry.Value),
                    StringComparer.Ordinal),
            _ => null
        };
    }

    private static bool TryParseInlineFunctionExpression(string input, out DscFunctionExpression expression)
    {
        expression = new DscFunctionExpression();
        var trimmed = input.Trim();
        if (!trimmed.StartsWith('[') || !trimmed.EndsWith(']'))
        {
            return false;
        }

        return TryParseFunctionCall(trimmed[1..^1].Trim(), out expression);
    }

    private static bool TryParseFunctionCall(string input, out DscFunctionExpression expression)
    {
        expression = new DscFunctionExpression();
        var parenIndex = input.IndexOf('(');
        if (parenIndex <= 0 || !input.EndsWith(')'))
        {
            return false;
        }

        var functionName = input[..parenIndex].Trim();
        if (string.IsNullOrWhiteSpace(functionName))
        {
            return false;
        }

        var argsText = input[(parenIndex + 1)..^1];
        var args = SplitArguments(argsText)
            .Select(ParseFunctionArgument)
            .ToList();

        expression = new DscFunctionExpression
        {
            FunctionName = functionName,
            Args = args
        };

        return true;
    }

    private static IEnumerable<string> SplitArguments(string argsText)
    {
        if (string.IsNullOrWhiteSpace(argsText))
        {
            yield break;
        }

        var start = 0;
        var parenDepth = 0;
        var bracketDepth = 0;
        var inSingleQuote = false;

        for (var i = 0; i < argsText.Length; i++)
        {
            var c = argsText[i];

            if (c == '\'')
            {
                if (inSingleQuote && i + 1 < argsText.Length && argsText[i + 1] == '\'')
                {
                    i++;
                    continue;
                }

                inSingleQuote = !inSingleQuote;
                continue;
            }

            if (inSingleQuote)
            {
                continue;
            }

            if (c == '(') parenDepth++;
            else if (c == ')') parenDepth--;
            else if (c == '[') bracketDepth++;
            else if (c == ']') bracketDepth--;
            else if (c == ',' && parenDepth == 0 && bracketDepth == 0)
            {
                yield return argsText[start..i].Trim();
                start = i + 1;
            }
        }

        yield return argsText[start..].Trim();
    }

    private static FunctionArg ParseFunctionArgument(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return FunctionArg.Literal(null);
        }

        if (TryParseFunctionCall(token, out var nestedExpression))
        {
            return FunctionArg.Nested(nestedExpression);
        }

        if (token.StartsWith('\'') && token.EndsWith('\'') && token.Length >= 2)
        {
            var unwrapped = token[1..^1]
                .Replace("''", "'")
                .Replace("\\'", "'");
            return FunctionArg.Literal(unwrapped);
        }

        if (token.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return FunctionArg.Literal(true);
        }

        if (token.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            return FunctionArg.Literal(false);
        }

        if (token.Equals("null()", StringComparison.OrdinalIgnoreCase) ||
            token.Equals("null", StringComparison.OrdinalIgnoreCase))
        {
            return FunctionArg.Literal(null);
        }

        if (long.TryParse(token, out var longValue))
        {
            return FunctionArg.Literal(longValue);
        }

        if (double.TryParse(token, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var doubleValue))
        {
            return FunctionArg.Literal(doubleValue);
        }

        return FunctionArg.Literal(token);
    }

    private static object? ParseScalarValue(YamlNode node)
    {
        if (node is not YamlScalarNode scalar) return null;
        var s = scalar.Value;
        if (s is null) return null;
        if (s == "null") return null;
        if (s == "true") return true;
        if (s == "false") return false;
        if (long.TryParse(s, out var l)) return l;
        if (double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var d)) return d;
        return s;
    }

    private static string? TryGetString(YamlMappingNode map, string key)
    {
        if (map.Children.TryGetValue(new YamlScalarNode(key), out var node) &&
            node is YamlScalarNode scalar)
        {
            return scalar.Value;
        }
        return null;
    }
}
