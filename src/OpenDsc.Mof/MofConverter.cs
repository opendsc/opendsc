// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Nodes;

using Kingsland.MofParser;
using Kingsland.MofParser.Models.Values;
using Kingsland.MofParser.Parsing;

using OpenDsc.Schema;

namespace OpenDsc.Mof;

/// <summary>
/// Converts DSC v1 compiled MOF files to DSC v3 configuration documents.
/// </summary>
public static class MofConverter
{
    private static readonly HashSet<string> MetaProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "ResourceID",
        "ModuleName",
        "ModuleVersion",
        "ConfigurationName",
        "SourceInfo",
        "DependsOn"
    };

    /// <summary>
    /// Converts a DSC v1 compiled MOF file to a DSC v3 configuration document.
    /// </summary>
    /// <param name="filePath">The path to the compiled <c>.mof</c> file.</param>
    /// <returns>A <see cref="DscConfigDocument"/> containing the converted resource instances.</returns>
    public static DscConfigDocument Convert(string filePath)
    {
        var instances = PowerShellDscHelper.ParseMofFileInstances(filePath);
        return ConvertInstances(instances);
    }

    /// <summary>
    /// Converts DSC v1 MOF text to a DSC v3 configuration document.
    /// </summary>
    /// <param name="mofText">The MOF text to parse and convert.</param>
    /// <returns>A <see cref="DscConfigDocument"/> containing the converted resource instances.</returns>
    public static DscConfigDocument ConvertText(string mofText)
    {
        var module = Parser.ParseText(mofText);
        return ConvertInstances(module.GetInstances());
    }

    private static DscConfigDocument ConvertInstances(IEnumerable<InstanceValue> instances)
    {
        var allInstances = new List<InstanceValue>(instances);

        foreach (var instance in allInstances)
        {
            if (instance.TypeName.StartsWith("OMI_ConfigurationDocument", StringComparison.OrdinalIgnoreCase)
                && instance.Properties.TryGetValue("ContentType", out var contentType)
                && contentType is StringValue { Value: "PasswordEncrypted" })
            {
                throw new NotSupportedException(
                    "This MOF file contains encrypted credentials (ContentType=\"PasswordEncrypted\") and cannot be converted. " +
                    "Decrypt the credentials on the target node first using Unprotect-CmsMessage before importing.");
            }
        }

        var aliasMap = BuildAliasMap(allInstances);

        var resourceInstances = new List<InstanceValue>();
        foreach (var instance in allInstances)
        {
            if (!instance.TypeName.StartsWith("OMI_ConfigurationDocument", StringComparison.OrdinalIgnoreCase)
                && instance.Properties.ContainsKey("ResourceID"))
            {
                resourceInstances.Add(instance);
            }
        }

        var friendlyToModule = BuildFriendlyNameMap(resourceInstances);

        var resources = new List<DscConfigResource>(resourceInstances.Count);
        foreach (var instance in resourceInstances)
        {
            var resource = ConvertInstance(instance, friendlyToModule, aliasMap);
            if (resource is not null)
            {
                resources.Add(resource);
            }
        }

        return new DscConfigDocument { Resources = resources };
    }

    private static Dictionary<string, InstanceValue> BuildAliasMap(List<InstanceValue> instances)
    {
        var map = new Dictionary<string, InstanceValue>(StringComparer.OrdinalIgnoreCase);
        foreach (var instance in instances)
        {
            if (!string.IsNullOrEmpty(instance.Alias))
            {
                map[instance.Alias] = instance;
            }
        }
        return map;
    }

    private static Dictionary<string, string> BuildFriendlyNameMap(List<InstanceValue> instances)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var instance in instances)
        {
            if (!TryParseResourceId(instance, out var friendlyName, out _))
            {
                continue;
            }

            var moduleName = GetStringProperty(instance, "ModuleName");
            if (moduleName is not null && !map.ContainsKey(friendlyName))
            {
                map[friendlyName] = moduleName;
            }
        }
        return map;
    }

    private static DscConfigResource? ConvertInstance(InstanceValue instance, Dictionary<string, string> friendlyToModule, Dictionary<string, InstanceValue> aliasMap)
    {
        if (!TryParseResourceId(instance, out var friendlyName, out var instanceName))
        {
            return null;
        }

        var moduleName = GetStringProperty(instance, "ModuleName") ?? friendlyName;
        var type = $"{moduleName}/{friendlyName}";

        var properties = new Dictionary<string, JsonNode?>();
        foreach (var kvp in instance.Properties)
        {
            if (!MetaProperties.Contains(kvp.Key))
            {
                properties[kvp.Key] = MofPropertyConverter.Convert(kvp.Value, aliasMap);
            }
        }

        var dependsOn = ConvertDependsOn(instance, friendlyToModule);

        return new DscConfigResource
        {
            Type = type,
            Name = instanceName,
            Properties = properties,
            DependsOn = dependsOn
        };
    }

    private static IReadOnlyList<string>? ConvertDependsOn(InstanceValue instance, Dictionary<string, string> friendlyToModule)
    {
        if (!instance.Properties.TryGetValue("DependsOn", out var depValue))
        {
            return null;
        }

        var deps = new List<string>();

        if (depValue is StringValue sv)
        {
            var expr = ParseDependsOnEntry(sv.Value, friendlyToModule);
            if (expr is not null)
            {
                deps.Add(expr);
            }
        }
        else if (depValue is LiteralValueArray arr)
        {
            foreach (var item in arr.Values)
            {
                if (item is StringValue itemSv)
                {
                    var expr = ParseDependsOnEntry(itemSv.Value, friendlyToModule);
                    if (expr is not null)
                    {
                        deps.Add(expr);
                    }
                }
            }
        }

        return deps.Count > 0 ? deps : null;
    }

    private static string? ParseDependsOnEntry(string value, Dictionary<string, string> friendlyToModule)
    {
        // Format: "[FriendlyName]InstanceName"
        if (value.Length < 3 || value[0] != '[')
        {
            return null;
        }

        var closingBracket = value.IndexOf(']');
        if (closingBracket < 2)
        {
            return null;
        }

        var depFriendlyName = value.Substring(1, closingBracket - 1);
        var depInstanceName = value.Substring(closingBracket + 1);

        var depModuleName = friendlyToModule.TryGetValue(depFriendlyName, out var mod)
            ? mod
            : depFriendlyName;

        return $"[resourceId('{depModuleName}/{depFriendlyName}', '{depInstanceName}')]";
    }

    private static bool TryParseResourceId(InstanceValue instance, out string friendlyName, out string instanceName)
    {
        friendlyName = string.Empty;
        instanceName = string.Empty;

        if (!instance.Properties.TryGetValue("ResourceID", out var ridValue) || ridValue is not StringValue sv)
        {
            return false;
        }

        var rid = sv.Value;
        if (rid.Length < 3 || rid[0] != '[')
        {
            return false;
        }

        var closingBracket = rid.IndexOf(']');
        if (closingBracket < 2)
        {
            return false;
        }

        friendlyName = rid.Substring(1, closingBracket - 1);
        instanceName = rid.Substring(closingBracket + 1);
        return true;
    }

    private static string? GetStringProperty(InstanceValue instance, string name)
    {
        if (instance.Properties.TryGetValue(name, out var value) && value is StringValue sv)
        {
            return sv.Value;
        }
        return null;
    }
}
