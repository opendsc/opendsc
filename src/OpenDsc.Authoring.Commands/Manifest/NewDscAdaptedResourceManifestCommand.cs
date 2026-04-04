// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Management.Automation;

namespace OpenDsc.Authoring.Commands;

/// <summary>
/// Creates adapted resource manifest objects from class-based PowerShell DSC resources.
/// </summary>
[Cmdlet(VerbsCommon.New, "DscAdaptedResourceManifest")]
[OutputType(typeof(DscAdaptedResourceManifest))]
public sealed class NewDscAdaptedResourceManifestCommand : PSCmdlet
{
    /// <summary>
    /// The path to a .ps1, .psm1, or .psd1 file containing class-based DSC resources.
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

            var ext = System.IO.Path.GetExtension(resolvedPath);
            if (!string.Equals(ext, ".ps1", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(ext, ".psm1", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(ext, ".psd1", StringComparison.OrdinalIgnoreCase))
            {
                WriteError(new ErrorRecord(
                    new ArgumentException($"Path '{resolvedPath}' must be a .ps1, .psm1, or .psd1 file."),
                    "InvalidFileExtension",
                    ErrorCategory.InvalidArgument,
                    resolvedPath));
                continue;
            }

            ModuleInfo moduleInfo;
            try
            {
                moduleInfo = DscResourceParser.ResolveModuleInfo(resolvedPath);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ModuleResolutionFailed", ErrorCategory.InvalidData, resolvedPath));
                continue;
            }

            if (!System.IO.File.Exists(moduleInfo.ScriptPath))
            {
                WriteError(new ErrorRecord(
                    new System.IO.FileNotFoundException($"Cannot find script file '{moduleInfo.ScriptPath}' to parse."),
                    "ScriptNotFound",
                    ErrorCategory.ObjectNotFound,
                    moduleInfo.ScriptPath));
                continue;
            }

            List<DscResourceTypeInfo> dscTypes;
            try
            {
                dscTypes = DscResourceParser.GetDscResourceTypeDefinitions(moduleInfo.ScriptPath);
            }
            catch (Exception ex)
            {
                WriteError(new ErrorRecord(ex, "ParseFailed", ErrorCategory.ParserError, moduleInfo.ScriptPath));
                continue;
            }

            if (dscTypes.Count == 0)
            {
                WriteWarning($"No class-based DSC resources found in '{Path}'.");
                continue;
            }

            var classHelpMap = DscResourceParser.GetClassCommentBasedHelp(moduleInfo.ScriptPath);

            foreach (var typeInfo in dscTypes)
            {
                var resourceName = typeInfo.TypeDefinitionAst.Name;

                WriteVerbose($"Processing DSC resource '{moduleInfo.ModuleName}/{resourceName}'");

                if (classHelpMap.TryGetValue(resourceName, out var classHelp))
                {
                    var properties = DscResourceParser.GetDscResourceProperties(
                        typeInfo.AllTypeDefinitions, typeInfo.TypeDefinitionAst);

                    var missingParams = properties
                        .Where(p => !classHelp.Parameters.ContainsKey(p.Name))
                        .Select(p => p.Name)
                        .ToArray();

                    if (missingParams.Length > 0)
                    {
                        WriteWarning($"Class '{resourceName}' comment-based help is missing .PARAMETER documentation for: {string.Join(", ", missingParams)}");
                    }
                }
                else
                {
                    WriteWarning($"No comment-based help found above class '{resourceName}'. Using default descriptions.");
                }

                var manifest = DscResourceParser.CreateAdaptedResourceManifest(moduleInfo, typeInfo, classHelpMap);
                WriteObject(manifest);
            }
        }
    }
}
