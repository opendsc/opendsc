// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections;
using System.Collections.Specialized;
using System.Management.Automation.Language;
using System.Text.RegularExpressions;

namespace OpenDsc.Authoring.Commands;

internal static class DscResourceParser
{
    private const string AdaptedResourceSchemaUri = "https://aka.ms/dsc/schemas/v3/bundled/adaptedresource/manifest.json";
    private const string JsonSchemaUri = "https://json-schema.org/draft/2020-12/schema";
    private const string DefaultAdapter = "Microsoft.Adapter/PowerShell";

    private static readonly string[] s_availableMethods =
        ["get", "set", "sethandlesexist", "whatif", "test", "delete", "export"];

    public static List<DscResourceTypeInfo> GetDscResourceTypeDefinitions(string path)
    {
        var ast = Parser.ParseFile(path, out _, out ParseError[] errors);

        if (errors.Length > 0)
        {
            throw new InvalidOperationException(
                $"Parse error in '{path}': {errors[0].Message}");
        }

        var allTypeDefinitions = ast.FindAll(
            a => a is TypeDefinitionAst, searchNestedScriptBlocks: false)
            .Cast<TypeDefinitionAst>()
            .ToArray();

        var results = new List<DscResourceTypeInfo>();

        foreach (var typeDefinition in allTypeDefinitions)
        {
            foreach (var attribute in typeDefinition.Attributes)
            {
                if (string.Equals(attribute.TypeName.Name, "DscResource", StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new DscResourceTypeInfo(typeDefinition, allTypeDefinitions));
                    break;
                }
            }
        }

        return results;
    }

    public static string[] GetDscResourceCapabilities(IReadOnlyList<MemberAst> members)
    {
        var capabilities = new List<string>();

        foreach (var member in members)
        {
            if (member is not FunctionMemberAst functionMember)
            {
                continue;
            }

            var name = functionMember.Name.ToLowerInvariant();
            if (!s_availableMethods.Contains(name))
            {
                continue;
            }

            var capability = name switch
            {
                "sethandlesexist" => "setHandlesExist",
                "whatif" => "whatIf",
                _ => name,
            };

            if (!capabilities.Contains(capability))
            {
                capabilities.Add(capability);
            }
        }

        return [.. capabilities];
    }

    public static List<DscPropertyInfo> GetDscResourceProperties(
        TypeDefinitionAst[] allTypeDefinitions,
        TypeDefinitionAst typeDefinitionAst)
    {
        var properties = new List<DscPropertyInfo>();
        CollectAstProperties(allTypeDefinitions, typeDefinitionAst, properties);
        return properties;
    }

    private static void CollectAstProperties(
        TypeDefinitionAst[] allTypeDefinitions,
        TypeDefinitionAst typeAst,
        List<DscPropertyInfo> properties)
    {
        foreach (var typeConstraint in typeAst.BaseTypes)
        {
            var baseType = allTypeDefinitions.FirstOrDefault(
                t => string.Equals(t.Name, typeConstraint.TypeName.Name, StringComparison.OrdinalIgnoreCase));

            if (baseType is not null)
            {
                CollectAstProperties(allTypeDefinitions, baseType, properties);
            }
        }

        foreach (var member in typeAst.Members)
        {
            if (member is not PropertyMemberAst propertyAst || propertyAst.IsStatic)
            {
                continue;
            }

            var isDscProperty = false;
            var isKey = false;
            var isMandatory = false;

            foreach (var attr in propertyAst.Attributes)
            {
                if (!string.Equals(attr.TypeName.Name, "DscProperty", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                isDscProperty = true;

                foreach (var namedArg in attr.NamedArguments)
                {
                    if (string.Equals(namedArg.ArgumentName, "Key", StringComparison.OrdinalIgnoreCase))
                    {
                        isKey = true;
                    }
                    else if (string.Equals(namedArg.ArgumentName, "Mandatory", StringComparison.OrdinalIgnoreCase))
                    {
                        isMandatory = true;
                    }
                }
            }

            if (!isDscProperty)
            {
                continue;
            }

            var typeName = propertyAst.PropertyType?.TypeName.Name ?? "string";

            string[]? enumValues = null;
            var enumAst = allTypeDefinitions.FirstOrDefault(
                t => string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase) && t.IsEnum);

            if (enumAst is not null)
            {
                enumValues = enumAst.Members.Select(m => m.Name).ToArray();
            }

            properties.Add(new DscPropertyInfo
            {
                Name = propertyAst.Name,
                TypeName = typeName,
                IsKey = isKey,
                IsMandatory = isMandatory || isKey,
                EnumValues = enumValues,
            });
        }
    }

    public static Dictionary<string, CommentBasedHelp> GetClassCommentBasedHelp(string path)
    {
        Parser.ParseFile(path, out Token[] tokens, out _);

        var blockCommentTokens = tokens
            .Where(t => t.Kind == TokenKind.Comment && t.Text.StartsWith("<#"))
            .ToArray();

        var classTokens = tokens
            .Where(t => t.Kind == TokenKind.Class)
            .ToArray();

        var result = new Dictionary<string, CommentBasedHelp>();

        foreach (var classToken in classTokens)
        {
            var classLine = classToken.Extent.StartLineNumber;
            Token? nearestComment = null;

            foreach (var commentToken in blockCommentTokens)
            {
                var gap = classLine - commentToken.Extent.EndLineNumber;
                if (gap < 1 || gap > 10)
                {
                    continue;
                }

                var isValid = true;
                foreach (var otherClass in classTokens)
                {
                    if (otherClass != classToken &&
                        otherClass.Extent.StartLineNumber > commentToken.Extent.EndLineNumber &&
                        otherClass.Extent.StartLineNumber < classLine)
                    {
                        isValid = false;
                        break;
                    }
                }

                if (isValid && (nearestComment is null ||
                    commentToken.Extent.EndLineNumber > nearestComment.Extent.EndLineNumber))
                {
                    nearestComment = commentToken;
                }
            }

            if (nearestComment is null)
            {
                continue;
            }

            var parsed = ParseCommentBasedHelp(nearestComment.Text);

            if (string.IsNullOrEmpty(parsed.Synopsis) &&
                string.IsNullOrEmpty(parsed.Description) &&
                parsed.Parameters.Count == 0)
            {
                continue;
            }

            var classIndex = Array.IndexOf(tokens, classToken);
            string? className = null;

            for (var i = classIndex + 1; i < tokens.Length; i++)
            {
                if (tokens[i].Kind == TokenKind.Identifier)
                {
                    className = tokens[i].Text;
                    break;
                }
            }

            if (className is not null)
            {
                result[className] = parsed;
            }
        }

        return result;
    }

    internal static CommentBasedHelp ParseCommentBasedHelp(string commentText)
    {
        var text = Regex.Replace(commentText, @"^\s*<#", string.Empty);
        text = Regex.Replace(text, @"#>\s*$", string.Empty);

        var result = new CommentBasedHelp();

        var keywordPattern = @"(?mi)^\s*\.(?<keyword>SYNOPSIS|DESCRIPTION|PARAMETER|EXAMPLE|NOTES|OUTPUTS|INPUTS|LINK|COMPONENT|ROLE|FUNCTIONALITY)[^\S\r\n]*(?<arg>.*)$";
        var matches = Regex.Matches(text, keywordPattern);

        if (matches.Count == 0)
        {
            return result;
        }

        for (var i = 0; i < matches.Count; i++)
        {
            var keyword = matches[i].Groups["keyword"].Value.ToUpperInvariant();
            var arg = matches[i].Groups["arg"].Value.Trim();

            var startIndex = matches[i].Index + matches[i].Length;
            var endIndex = i + 1 < matches.Count ? matches[i + 1].Index : text.Length;
            var content = text[startIndex..endIndex].Trim();

            switch (keyword)
            {
                case "SYNOPSIS":
                    result.Synopsis = content;
                    break;
                case "DESCRIPTION":
                    result.Description = content;
                    break;
                case "PARAMETER":
                    if (!string.IsNullOrWhiteSpace(arg))
                    {
                        result.Parameters[arg] = content;
                    }
                    break;
            }
        }

        return result;
    }

    public static OrderedDictionary ConvertToJsonSchemaType(string typeName)
    {
        return typeName.ToLowerInvariant() switch
        {
            "string" => new OrderedDictionary { ["type"] = "string" },
            "int" or "int32" or "int64" or "long" => new OrderedDictionary { ["type"] = "integer" },
            "double" or "float" or "single" or "decimal" => new OrderedDictionary { ["type"] = "number" },
            "bool" or "boolean" or "switch" => new OrderedDictionary { ["type"] = "boolean" },
            "hashtable" => new OrderedDictionary { ["type"] = "object" },
            "datetime" => new OrderedDictionary { ["type"] = "string", ["format"] = "date-time" },
            _ when typeName.EndsWith("[]") =>
                new OrderedDictionary
                {
                    ["type"] = "array",
                    ["items"] = ConvertToJsonSchemaType(typeName[..^2]),
                },
            _ => new OrderedDictionary { ["type"] = "string" },
        };
    }

    public static OrderedDictionary BuildEmbeddedJsonSchema(
        string resourceName,
        List<DscPropertyInfo> properties,
        string? description,
        CommentBasedHelp? classHelp)
    {
        var schemaProperties = new OrderedDictionary();
        var requiredList = new List<string>();

        foreach (var prop in properties)
        {
            var schemaProp = new OrderedDictionary();

            if (prop.EnumValues is not null)
            {
                schemaProp["type"] = "string";
                schemaProp["enum"] = prop.EnumValues;
            }
            else
            {
                var jsonType = ConvertToJsonSchemaType(prop.TypeName);
                foreach (DictionaryEntry entry in jsonType)
                {
                    schemaProp[entry.Key] = entry.Value;
                }
            }

            schemaProp["title"] = prop.Name;

            if (classHelp is not null && classHelp.Parameters.TryGetValue(prop.Name, out var paramDescription))
            {
                schemaProp["description"] = paramDescription;
            }
            else
            {
                schemaProp["description"] = $"The {prop.Name} property.";
            }

            schemaProperties[prop.Name] = schemaProp;

            if (prop.IsMandatory)
            {
                requiredList.Add(prop.Name);
            }
        }

        var schema = new OrderedDictionary
        {
            ["$schema"] = JsonSchemaUri,
            ["title"] = resourceName,
            ["type"] = "object",
            ["required"] = requiredList.ToArray(),
            ["additionalProperties"] = false,
            ["properties"] = schemaProperties,
        };

        if (!string.IsNullOrEmpty(description))
        {
            schema["description"] = description;
        }

        return schema;
    }

    public static ModuleInfo ResolveModuleInfo(string path)
    {
        var resolvedPath = Path.GetFullPath(path);
        var extension = Path.GetExtension(resolvedPath);
        var directory = Path.GetDirectoryName(resolvedPath)!;

        if (string.Equals(extension, ".psd1", StringComparison.OrdinalIgnoreCase))
        {
            return ResolveFromManifest(resolvedPath, directory);
        }

        var moduleName = Path.GetFileNameWithoutExtension(resolvedPath);
        var psd1Path = Path.Combine(directory, $"{moduleName}.psd1");

        if (File.Exists(psd1Path))
        {
            return ResolveFromManifest(psd1Path, directory);
        }

        var fileName = Path.GetFileName(resolvedPath);

        return new ModuleInfo
        {
            ModuleName = moduleName,
            Version = "0.0.1",
            Author = string.Empty,
            Description = string.Empty,
            ScriptPath = resolvedPath,
            Psd1Path = fileName,
            Directory = directory,
        };
    }

    private static ModuleInfo ResolveFromManifest(string psd1Path, string directory)
    {
        var ast = Parser.ParseFile(psd1Path, out _, out _);

        var hashtableAst = ast.Find(a => a is HashtableAst, searchNestedScriptBlocks: false) as HashtableAst;

        var moduleName = Path.GetFileNameWithoutExtension(psd1Path);
        string version = "0.0.1";
        string author = string.Empty;
        string description = string.Empty;
        string? rootModule = null;

        if (hashtableAst is not null)
        {
            foreach (var kvp in hashtableAst.KeyValuePairs)
            {
                var key = kvp.Item1 is StringConstantExpressionAst strKey ? strKey.Value : null;
                var value = kvp.Item2 is PipelineAst pipeline &&
                            pipeline.PipelineElements.Count == 1 &&
                            pipeline.PipelineElements[0] is CommandExpressionAst cmdExpr
                    ? GetConstantValue(cmdExpr.Expression)
                    : null;

                switch (key)
                {
                    case "ModuleVersion":
                        version = value ?? version;
                        break;
                    case "Author":
                        author = value ?? author;
                        break;
                    case "Description":
                        description = value ?? description;
                        break;
                    case "RootModule":
                        rootModule = value;
                        break;
                }
            }
        }

        if (string.IsNullOrEmpty(rootModule))
        {
            rootModule = $"{moduleName}.psm1";
        }

        var scriptPath = Path.Combine(directory, rootModule);
        var psd1RelativePath = Path.GetFileName(psd1Path);

        return new ModuleInfo
        {
            ModuleName = moduleName,
            Version = version,
            Author = author,
            Description = description,
            ScriptPath = scriptPath,
            Psd1Path = psd1RelativePath,
            Directory = directory,
        };
    }

    private static string? GetConstantValue(ExpressionAst expression)
    {
        return expression switch
        {
            StringConstantExpressionAst str => str.Value,
            ConstantExpressionAst constant => constant.Value?.ToString(),
            _ => null,
        };
    }

    public static DscAdaptedResourceManifest ConvertToAdaptedResourceManifest(OrderedDictionary hashtable)
    {
        var manifest = new DscAdaptedResourceManifest
        {
            Schema = hashtable["$schema"]?.ToString() ?? string.Empty,
            Type = hashtable["type"]?.ToString() ?? string.Empty,
            Kind = hashtable.Contains("kind") ? hashtable["kind"]?.ToString() ?? "resource" : "resource",
            Version = hashtable["version"]?.ToString() ?? string.Empty,
            RequireAdapter = hashtable["requireAdapter"]?.ToString() ?? string.Empty,
        };

        if (hashtable.Contains("capabilities") && hashtable["capabilities"] is object[] caps)
        {
            manifest.Capabilities = caps.Select(c => c.ToString()!).ToArray();
        }

        if (hashtable.Contains("description"))
        {
            manifest.Description = hashtable["description"]?.ToString() ?? string.Empty;
        }

        if (hashtable.Contains("author"))
        {
            manifest.Author = hashtable["author"]?.ToString() ?? string.Empty;
        }

        if (hashtable.Contains("path"))
        {
            manifest.Path = hashtable["path"]?.ToString() ?? string.Empty;
        }

        if (hashtable.Contains("schema") && hashtable["schema"] is OrderedDictionary schemaData)
        {
            var embedded = schemaData.Contains("embedded")
                ? schemaData["embedded"] as OrderedDictionary ?? []
                : schemaData;

            manifest.ManifestSchema = new DscAdaptedResourceManifestSchemaWrapper { Embedded = embedded };
        }

        return manifest;
    }

    public static DscAdaptedResourceManifest CreateAdaptedResourceManifest(
        ModuleInfo moduleInfo,
        DscResourceTypeInfo typeInfo,
        Dictionary<string, CommentBasedHelp>? classHelpMap)
    {
        var resourceName = typeInfo.TypeDefinitionAst.Name;
        var resourceType = $"{moduleInfo.ModuleName}/{resourceName}";

        var capabilities = GetDscResourceCapabilities(typeInfo.TypeDefinitionAst.Members);
        var properties = GetDscResourceProperties(typeInfo.AllTypeDefinitions, typeInfo.TypeDefinitionAst);

        CommentBasedHelp? classHelp = null;
        var resourceDescription = moduleInfo.Description;

        if (classHelpMap is not null && classHelpMap.TryGetValue(resourceName, out classHelp))
        {
            if (!string.IsNullOrWhiteSpace(classHelp.Synopsis))
            {
                resourceDescription = classHelp.Synopsis;
            }
            else if (!string.IsNullOrWhiteSpace(classHelp.Description))
            {
                resourceDescription = classHelp.Description;
            }
        }

        var embeddedSchema = BuildEmbeddedJsonSchema(resourceType, properties, resourceDescription, classHelp);

        return new DscAdaptedResourceManifest
        {
            Schema = AdaptedResourceSchemaUri,
            Type = resourceType,
            Kind = "resource",
            Version = moduleInfo.Version,
            Capabilities = capabilities,
            Description = resourceDescription,
            Author = moduleInfo.Author,
            RequireAdapter = DefaultAdapter,
            Path = moduleInfo.Psd1Path,
            ManifestSchema = new DscAdaptedResourceManifestSchemaWrapper { Embedded = embeddedSchema },
        };
    }
}
