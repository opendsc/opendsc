// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Nodes;

using Kingsland.MofParser.Models.Types;
using Kingsland.MofParser.Models.Values;
using Kingsland.MofParser.Parsing;

namespace OpenDsc.Mof;

/// <summary>
/// Converts MOF class schema definitions to JSON Schema objects.
/// </summary>
public static class MofSchemaConverter
{
    /// <summary>
    /// Converts a MOF schema file to a JSON Schema object representing the primary DSC resource class.
    /// </summary>
    /// <param name="filePath">The path to the <c>.schema.mof</c> file.</param>
    /// <returns>
    /// A <see cref="JsonObject"/> containing the JSON Schema (draft-2020-12) for the primary resource
    /// class, with referenced helper classes placed in <c>$defs</c>.
    /// </returns>
    public static JsonObject Convert(string filePath)
    {
        var text = File.ReadAllText(filePath);
        return ConvertText(text);
    }

    /// <summary>
    /// Converts MOF schema text to a JSON Schema object representing the primary DSC resource class.
    /// </summary>
    /// <param name="mofText">The MOF schema text to parse.</param>
    /// <returns>
    /// A <see cref="JsonObject"/> containing the JSON Schema (draft-2020-12) for the primary resource
    /// class, with referenced helper classes placed in <c>$defs</c>.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no class inheriting from <c>OMI_BaseResource</c> is found in the MOF text.
    /// </exception>
    public static JsonObject ConvertText(string mofText)
    {
        var module = Parser.ParseText(mofText);
        var classes = module.GetClasses().ToList();

        var primary = classes.FirstOrDefault(c =>
            string.Equals(c.SuperClass, "OMI_BaseResource", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                "No class inheriting from OMI_BaseResource was found in the MOF text.");

        var classLookup = classes
            .Where(c => !string.Equals(c.SuperClass, "OMI_BaseResource", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

        var referencedClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var (properties, required) = BuildProperties(primary, classLookup, referencedClasses);

        var schema = new JsonObject
        {
            ["$schema"] = "https://json-schema.org/draft/2020-12/schema",
            ["title"] = primary.Name,
            ["type"] = "object"
        };

        if (required.Count > 0)
        {
            schema["required"] = ToJsonArray(required);
        }

        schema["properties"] = properties;

        var defs = BuildDefs(referencedClasses, classLookup);
        if (defs.Count > 0)
        {
            schema["$defs"] = defs;
        }

        return schema;
    }

    private static (JsonObject properties, List<string> required) BuildProperties(
        Class cls,
        IReadOnlyDictionary<string, Class> classLookup,
        HashSet<string> referencedClasses)
    {
        var required = new List<string>();
        var properties = new JsonObject();

        foreach (var prop in cls.GetProperties())
        {
            if (prop.Qualifiers.Any(q =>
                string.Equals(q.Name, "Key", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(q.Name, "Required", StringComparison.OrdinalIgnoreCase)))
            {
                required.Add(prop.Name);
            }

            properties[prop.Name] = BuildPropertySchema(prop, classLookup, referencedClasses);
        }

        return (properties, required);
    }

    private static JsonObject BuildPropertySchema(
        Property prop,
        IReadOnlyDictionary<string, Class> classLookup,
        HashSet<string> referencedClasses)
    {
        var embeddedInstance = GetStringQualifierValue(prop, "EmbeddedInstance");
        var description = GetStringQualifierValue(prop, "Description");
        var valueMap = GetListQualifierValues(prop, "ValueMap");

        JsonObject itemSchema;
        if (embeddedInstance is not null)
        {
            referencedClasses.Add(embeddedInstance);
            itemSchema = new JsonObject { ["$ref"] = $"#/$defs/{embeddedInstance}" };
        }
        else
        {
            itemSchema = BuildTypeSchema(prop.ReturnType, classLookup, referencedClasses);

            if (valueMap is { Count: > 0 })
            {
                itemSchema["enum"] = ToJsonArray(valueMap);
            }
        }

        JsonObject propSchema = prop.IsArray
            ? new JsonObject { ["type"] = "array", ["items"] = itemSchema }
            : itemSchema;

        if (description is not null)
        {
            propSchema["description"] = description;
        }

        return propSchema;
    }

    private static JsonObject BuildTypeSchema(
        string returnType,
        IReadOnlyDictionary<string, Class> classLookup,
        HashSet<string> referencedClasses)
    {
        return returnType.ToLowerInvariant() switch
        {
            "string" => new JsonObject { ["type"] = "string" },
            "boolean" => new JsonObject { ["type"] = "boolean" },
            "uint8" or "uint16" or "uint32" or "uint64" =>
                new JsonObject { ["type"] = "integer", ["minimum"] = 0 },
            "sint8" or "sint16" or "sint32" or "sint64" =>
                new JsonObject { ["type"] = "integer" },
            "real32" or "real64" => new JsonObject { ["type"] = "number" },
            "datetime" => new JsonObject { ["type"] = "string", ["format"] = "date-time" },
            _ => BuildClassRefSchema(returnType, classLookup, referencedClasses)
        };
    }

    private static JsonObject BuildClassRefSchema(
        string typeName,
        IReadOnlyDictionary<string, Class> classLookup,
        HashSet<string> referencedClasses)
    {
        if (classLookup.ContainsKey(typeName))
        {
            referencedClasses.Add(typeName);
        }

        return new JsonObject { ["$ref"] = $"#/$defs/{typeName}" };
    }

    private static JsonObject BuildDefs(
        HashSet<string> referencedClasses,
        IReadOnlyDictionary<string, Class> classLookup)
    {
        var defs = new JsonObject();
        var toProcess = new Queue<string>(referencedClasses);
        var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (toProcess.Count > 0)
        {
            var className = toProcess.Dequeue();
            if (!processed.Add(className) || !classLookup.TryGetValue(className, out var cls))
            {
                continue;
            }

            var innerReferenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var (properties, required) = BuildProperties(cls, classLookup, innerReferenced);

            var classSchema = new JsonObject { ["type"] = "object" };
            if (required.Count > 0)
            {
                classSchema["required"] = ToJsonArray(required);
            }
            classSchema["properties"] = properties;

            defs[className] = classSchema;

            foreach (var inner in innerReferenced.Where(i => !processed.Contains(i)))
            {
                toProcess.Enqueue(inner);
            }
        }

        return defs;
    }

    private static string? GetStringQualifierValue(Property prop, string qualifierName)
    {
        var qualifier = prop.Qualifiers.FirstOrDefault(q =>
            string.Equals(q.Name, qualifierName, StringComparison.OrdinalIgnoreCase));
        return (qualifier?.Value as StringValue)?.Value;
    }

    private static IReadOnlyList<string>? GetListQualifierValues(Property prop, string qualifierName)
    {
        var qualifier = prop.Qualifiers.FirstOrDefault(q =>
            string.Equals(q.Name, qualifierName, StringComparison.OrdinalIgnoreCase));

        if (qualifier?.Value is LiteralValueArray lva)
        {
            return lva.Values.OfType<StringValue>().Select(sv => sv.Value).ToList();
        }

        return null;
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var arr = new JsonArray();
        foreach (var v in values)
        {
            arr.Add((JsonNode?)JsonValue.Create(v));
        }
        return arr;
    }
}
