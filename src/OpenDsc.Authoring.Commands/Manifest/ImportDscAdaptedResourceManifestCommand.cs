// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Specialized;
using System.Management.Automation;
using System.Text.Json;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Imports adapted resource manifest objects from .dsc.adaptedResource.json files.
/// </summary>
[Cmdlet(VerbsData.Import, "DscAdaptedResourceManifest")]
[OutputType(typeof(DscAdaptedResourceManifest))]
public sealed class ImportDscAdaptedResourceManifestCommand : PSCmdlet
{
    /// <summary>
    /// The path to a .dsc.adaptedResource.json file.
    /// </summary>
    [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
    [ValidateNotNullOrEmpty]
    [Alias("FullName")]
    public string Path { get; set; } = string.Empty;

    /// <inheritdoc/>
    protected override void ProcessRecord()
    {
        var resolvedPaths = GetResolvedProviderPathFromPSPath(Path, out _);

        foreach (var resolvedPath in resolvedPaths)
        {
            if (!System.IO.File.Exists(resolvedPath))
            {
                WriteError(new ErrorRecord(
                    new System.IO.FileNotFoundException($"Path '{resolvedPath}' does not exist."),
                    "PathNotFound",
                    ErrorCategory.ObjectNotFound,
                    resolvedPath));
                continue;
            }

            WriteVerbose($"Importing adapted resource manifest from '{resolvedPath}'");

            try
            {
                var jsonContent = System.IO.File.ReadAllText(resolvedPath);
                var hashtable = JsonToOrderedDictionary(jsonContent);
                var manifest = DscResourceParser.ConvertToAdaptedResourceManifest(hashtable);
                WriteObject(manifest);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ImportFailed", ErrorCategory.ReadError, resolvedPath));
            }
        }
    }

    internal static OrderedDictionary JsonToOrderedDictionary(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return ConvertJsonElement(doc.RootElement) as OrderedDictionary ?? [];
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertJsonObject(element),
            JsonValueKind.Array => ConvertJsonArray(element),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var l) => l,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static OrderedDictionary ConvertJsonObject(JsonElement element)
    {
        var result = new OrderedDictionary();
        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = ConvertJsonElement(property.Value);
        }
        return result;
    }

    private static object[] ConvertJsonArray(JsonElement element)
    {
        var items = new List<object?>();
        foreach (var item in element.EnumerateArray())
        {
            items.Add(ConvertJsonElement(item));
        }
        return items.ToArray()!;
    }
}
