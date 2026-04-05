// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;

using Json.Schema;

namespace OpenDsc.Authoring.Commands;

internal static class PsClassSchemaConverter
{
    private const string DscPropertyAttributeFullName =
        "System.Management.Automation.DscPropertyAttribute";

    private const string ValidateSetAttributeFullName =
        "System.Management.Automation.ValidateSetAttribute";

    private const string ValidatePatternAttributeFullName =
        "System.Management.Automation.ValidatePatternAttribute";

    private const string ValidateRangeAttributeFullName =
        "System.Management.Automation.ValidateRangeAttribute";

    internal static JsonSchema Convert(string resourceName, Type resourceType)
    {
        var referencedTypes = new HashSet<Type>();
        var (propertySchemas, requiredNames) = BuildClassProperties(resourceType, referencedTypes);

        var builder = new JsonSchemaBuilder()
            .Schema("https://json-schema.org/draft/2020-12/schema")
            .Title(resourceName)
            .Type(SchemaValueType.Object)
            .Properties(propertySchemas);

        if (requiredNames.Count > 0)
        {
            builder.Required(requiredNames);
        }

        var defs = BuildDefs(referencedTypes);
        if (defs.Count > 0)
        {
            builder.Defs(defs);
        }

        return builder.Build();
    }

    private static (IReadOnlyDictionary<string, JsonSchemaBuilder> properties, List<string> required)
        BuildClassProperties(Type type, HashSet<Type> referencedTypes)
    {
        var required = new List<string>();
        var properties = new Dictionary<string, JsonSchemaBuilder>();

        foreach (var prop in type.GetProperties(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            var attrs = prop.GetCustomAttributes().ToArray();

            var dscAttr = attrs.FirstOrDefault(a =>
                string.Equals(a.GetType().FullName, DscPropertyAttributeFullName, StringComparison.Ordinal));

            if (dscAttr is null)
            {
                continue;
            }

            var dscAttrType = dscAttr.GetType();
            bool isKey = (bool)(dscAttrType.GetProperty("Key")?.GetValue(dscAttr) ?? false);
            bool isMandatory = (bool)(dscAttrType.GetProperty("Mandatory")?.GetValue(dscAttr) ?? false);

            if (isKey || isMandatory)
            {
                required.Add(prop.Name);
            }

            properties[prop.Name] = BuildPropertySchema(prop, attrs, referencedTypes);
        }

        return (properties, required);
    }

    private static JsonSchemaBuilder BuildPropertySchema(
        PropertyInfo prop, object[] attrs, HashSet<Type> referencedTypes)
    {
        var propType = prop.PropertyType;
        bool isArray = propType.IsArray;
        var elementType = isArray ? propType.GetElementType()! : propType;

        var itemBuilder = BuildTypeSchema(elementType, referencedTypes);

        var validateSetAttr = attrs.FirstOrDefault(a =>
            string.Equals(a.GetType().FullName, ValidateSetAttributeFullName, StringComparison.Ordinal));

        if (validateSetAttr is not null)
        {
            var validValues = validateSetAttr.GetType()
                .GetProperty("ValidValues")
                ?.GetValue(validateSetAttr) as IEnumerable<string>;

            if (validValues is not null)
            {
                itemBuilder.Enum(validValues);
            }
        }

        var validatePatternAttr = attrs.FirstOrDefault(a =>
            string.Equals(a.GetType().FullName, ValidatePatternAttributeFullName, StringComparison.Ordinal));

        if (validatePatternAttr is not null)
        {
            var pattern = validatePatternAttr.GetType()
                .GetProperty("RegexPattern")
                ?.GetValue(validatePatternAttr) as string;

            if (pattern is not null)
            {
                itemBuilder.Pattern(pattern);
            }
        }

        if (IsNumericType(elementType))
        {
            var validateRangeAttr = attrs.FirstOrDefault(a =>
                string.Equals(a.GetType().FullName, ValidateRangeAttributeFullName, StringComparison.Ordinal));

            if (validateRangeAttr is not null)
            {
                var attrType = validateRangeAttr.GetType();
                var minRange = attrType.GetProperty("MinRange")?.GetValue(validateRangeAttr);
                var maxRange = attrType.GetProperty("MaxRange")?.GetValue(validateRangeAttr);

                if (minRange is not null)
                {
                    try { itemBuilder.Minimum(System.Convert.ToDecimal(minRange)); }
                    catch (Exception) { }
                }

                if (maxRange is not null)
                {
                    try { itemBuilder.Maximum(System.Convert.ToDecimal(maxRange)); }
                    catch (Exception) { }
                }
            }
        }

        var propBuilder = !isArray
            ? itemBuilder
            : new JsonSchemaBuilder()
                .Type(SchemaValueType.Array)
                .Items(itemBuilder);

        var dscAttr = attrs.FirstOrDefault(a =>
            string.Equals(a.GetType().FullName, DscPropertyAttributeFullName, StringComparison.Ordinal));

        if (dscAttr is not null)
        {
            bool isNotConfigurable = (bool)(dscAttr.GetType().GetProperty("NotConfigurable")?.GetValue(dscAttr) ?? false);
            if (isNotConfigurable)
            {
                propBuilder.ReadOnly(true);
            }
        }

        return propBuilder;
    }

    private static JsonSchemaBuilder BuildTypeSchema(Type type, HashSet<Type> referencedTypes)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            type = underlying;
        }

        if (type == typeof(string))
        {
            return new JsonSchemaBuilder().Type(SchemaValueType.String);
        }

        if (type == typeof(bool))
        {
            return new JsonSchemaBuilder().Type(SchemaValueType.Boolean);
        }

        if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
            type == typeof(sbyte) || type == typeof(byte))
        {
            return new JsonSchemaBuilder().Type(SchemaValueType.Integer);
        }

        if (type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort))
        {
            return new JsonSchemaBuilder()
                .Type(SchemaValueType.Integer)
                .Minimum(0);
        }

        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
        {
            return new JsonSchemaBuilder().Type(SchemaValueType.Number);
        }

        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
        {
            return new JsonSchemaBuilder()
                .Type(SchemaValueType.String)
                .Format("date-time");
        }

        if (type.IsEnum)
        {
            return new JsonSchemaBuilder()
                .Type(SchemaValueType.String)
                .Enum(Enum.GetNames(type));
        }

        if (type.IsArray)
        {
            var elementType = type.GetElementType()!;
            return new JsonSchemaBuilder()
                .Type(SchemaValueType.Array)
                .Items(BuildTypeSchema(elementType, referencedTypes));
        }

        if (IsDscClass(type))
        {
            referencedTypes.Add(type);
            return new JsonSchemaBuilder().Ref($"#/$defs/{type.Name}");
        }

        return new JsonSchemaBuilder().Type(SchemaValueType.Object);
    }

    private static bool IsDscClass(Type type)
    {
        return type.IsClass &&
               !type.IsPrimitive &&
               type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                   .Any(p => p.GetCustomAttributes()
                       .Any(a => string.Equals(
                           a.GetType().FullName, DscPropertyAttributeFullName, StringComparison.Ordinal)));
    }

    private static bool IsNumericType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t == typeof(int) || t == typeof(long) || t == typeof(short) ||
               t == typeof(sbyte) || t == typeof(byte) || t == typeof(uint) ||
               t == typeof(ulong) || t == typeof(ushort) || t == typeof(double) ||
               t == typeof(float) || t == typeof(decimal);
    }

    private static IReadOnlyDictionary<string, JsonSchemaBuilder> BuildDefs(
        HashSet<Type> referencedTypes)
    {
        var defs = new Dictionary<string, JsonSchemaBuilder>();
        var toProcess = new Queue<Type>(referencedTypes);
        var processed = new HashSet<Type>();

        while (toProcess.Count > 0)
        {
            var type = toProcess.Dequeue();
            if (!processed.Add(type))
            {
                continue;
            }

            var innerReferenced = new HashSet<Type>();
            var (properties, required) = BuildClassProperties(type, innerReferenced);

            var classBuilder = new JsonSchemaBuilder()
                .Type(SchemaValueType.Object)
                .Properties(properties);

            if (required.Count > 0)
            {
                classBuilder.Required(required);
            }

            defs[type.Name] = classBuilder;

            foreach (var inner in innerReferenced.Where(t => !processed.Contains(t)))
            {
                toProcess.Enqueue(inner);
            }
        }

        return defs;
    }
}
