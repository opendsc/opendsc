// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Globalization;
using System.Text.Json.Nodes;

using Kingsland.MofParser.Models.Values;

namespace OpenDsc.Mof;

internal static class MofPropertyConverter
{
    internal static JsonNode? Convert(PropertyValue value, IReadOnlyDictionary<string, InstanceValue>? aliasMap = null) => value switch
    {
        StringValue sv => JsonValue.Create(sv.Value),
        IntegerValue iv => JsonValue.Create(iv.Value),
        RealValue rv => ParseRealValue(rv),
        BooleanValue bv => JsonValue.Create(bv.Value),
        EnumValue ev => JsonValue.Create(ev.Literal),
        NullValue => null,
        LiteralValueArray lva => ConvertLiteralArray(lva, aliasMap),
        ComplexObjectValue cov => ConvertComplexObject(cov, aliasMap),
        ComplexValueArray cva => ConvertComplexValueArray(cva, aliasMap),
        EnumValueArray eva => ConvertEnumValueArray(eva),
        AliasValue av => ResolveAlias(av.Name, aliasMap),
        _ => JsonValue.Create(value.ToString())
    };

    internal static JsonObject ConvertInstance(InstanceValue instance, IReadOnlyDictionary<string, InstanceValue>? aliasMap)
    {
        var result = new JsonObject();
        foreach (var kvp in instance.Properties)
        {
            result[kvp.Key] = Convert(kvp.Value, aliasMap);
        }
        return result;
    }

    private static JsonNode? ResolveAlias(string name, IReadOnlyDictionary<string, InstanceValue>? aliasMap)
    {
        if (aliasMap is not null && aliasMap.TryGetValue(name, out var instance))
        {
            return ConvertInstance(instance, aliasMap);
        }

        return JsonValue.Create(name);
    }

    private static JsonNode ParseRealValue(RealValue rv)
    {
        var str = rv.ToString()!;
        return double.TryParse(str, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
            ? JsonValue.Create(d)
            : (JsonNode)JsonValue.Create(str)!;
    }

    private static JsonArray ConvertLiteralArray(LiteralValueArray array, IReadOnlyDictionary<string, InstanceValue>? aliasMap)
    {
        var result = new JsonArray();
        foreach (var item in array.Values)
        {
            result.Add(Convert(item, aliasMap));
        }
        return result;
    }

    private static JsonObject ConvertComplexObject(ComplexObjectValue obj, IReadOnlyDictionary<string, InstanceValue>? aliasMap)
    {
        var result = new JsonObject();
        foreach (var kvp in obj.Properties)
        {
            result[kvp.Key] = Convert(kvp.Value, aliasMap);
        }
        return result;
    }

    private static JsonArray ConvertComplexValueArray(ComplexValueArray array, IReadOnlyDictionary<string, InstanceValue>? aliasMap)
    {
        var result = new JsonArray();
        foreach (var item in array.Values)
        {
            JsonNode? node = item switch
            {
                ComplexObjectValue cov => ConvertComplexObject(cov, aliasMap),
                AliasValue av => ResolveAlias(av.Name, aliasMap),
                _ => (JsonNode?)JsonValue.Create(item.ToString())
            };
            result.Add(node);
        }
        return result;
    }

    private static JsonArray ConvertEnumValueArray(EnumValueArray array)
    {
        var result = new JsonArray();
        foreach (var item in array.Values)
        {
            result.Add((JsonNode?)JsonValue.Create(item.Literal));
        }
        return result;
    }
}


