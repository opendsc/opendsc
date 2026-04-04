// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Specialized;
using System.Management.Automation;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Imports a DSC resource manifest list from a .dsc.manifests.json file.
/// </summary>
[Cmdlet(VerbsData.Import, "DscResourceManifest")]
[OutputType(typeof(DscResourceManifestList))]
public sealed class ImportDscResourceManifestCommand : PSCmdlet
{
    /// <summary>
    /// The path to a .dsc.manifests.json file.
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

            WriteVerbose($"Importing resource manifest list from '{resolvedPath}'");

            try
            {
                var jsonContent = System.IO.File.ReadAllText(resolvedPath);
                var hashtable = ImportDscAdaptedResourceManifestCommand.JsonToOrderedDictionary(jsonContent);

                var manifestList = new DscResourceManifestList();

                if (hashtable.Contains("adaptedResources") && hashtable["adaptedResources"] is object[] adaptedArray)
                {
                    foreach (var item in adaptedArray)
                    {
                        if (item is OrderedDictionary ar)
                        {
                            var manifest = DscResourceParser.ConvertToAdaptedResourceManifest(ar);
                            manifestList.AddAdaptedResource(manifest);
                        }
                    }
                }

                if (hashtable.Contains("resources") && hashtable["resources"] is object[] resourceArray)
                {
                    foreach (var item in resourceArray)
                    {
                        if (item is OrderedDictionary res)
                        {
                            manifestList.AddResource(res);
                        }
                    }
                }

                if (hashtable.Contains("extensions") && hashtable["extensions"] is object[] extensionArray)
                {
                    foreach (var item in extensionArray)
                    {
                        if (item is OrderedDictionary ext)
                        {
                            manifestList.AddExtension(ext);
                        }
                    }
                }

                WriteObject(manifestList);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ImportFailed", ErrorCategory.ReadError, resolvedPath));
            }
        }
    }
}
