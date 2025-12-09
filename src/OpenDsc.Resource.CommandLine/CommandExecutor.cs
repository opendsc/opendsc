// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

namespace OpenDsc.Resource.CommandLine;

internal static class CommandExecutor
{
    internal static void ExecuteGet(ResourceRegistration registration, string input)
    {
        if (registration.GetAction == null)
        {
            throw new NotImplementedException($"Resource '{registration.Type}' does not support Get capability.");
        }

        registration.GetAction(input);
    }

    internal static void ExecuteSet(ResourceRegistration registration, string input)
    {
        if (registration.SetAction == null)
        {
            throw new NotImplementedException($"Resource '{registration.Type}' does not support Set capability.");
        }

        registration.SetAction(input);
    }

    internal static void ExecuteTest(ResourceRegistration registration, string input)
    {
        if (registration.TestAction == null)
        {
            throw new NotImplementedException($"Resource '{registration.Type}' does not support Test capability.");
        }

        registration.TestAction(input);
    }

    internal static void ExecuteDelete(ResourceRegistration registration, string input)
    {
        if (registration.DeleteAction == null)
        {
            throw new NotImplementedException($"Resource '{registration.Type}' does not support Delete capability.");
        }

        registration.DeleteAction(input);
    }

    internal static void ExecuteExport(ResourceRegistration registration)
    {
        if (registration.ExportAction == null)
        {
            throw new NotImplementedException($"Resource '{registration.Type}' does not support Export capability.");
        }

        registration.ExportAction();
    }

    internal static void ExecuteSchema(ResourceRegistration registration)
    {
        if (registration.SchemaFunc == null)
        {
            throw new InvalidOperationException($"Resource '{registration.Type}' does not have a schema func.");
        }

        var schema = registration.SchemaFunc();
        Console.WriteLine(schema);
    }

    internal static void GenerateSingleResourceManifest(ResourceRegistration registration, bool isMultiResourceExe, bool save)
    {
        var manifest = BuildResourceManifest(registration, isMultiResourceExe);
        var json = JsonSerializer.Serialize(manifest, typeof(DscResourceManifest), SourceGenerationContext.Default);

        if (save)
        {
            var fileName = GetSingleResourceManifestFileName(registration.Type);
#if NET6_0_OR_GREATER
            var entryLocation = Environment.ProcessPath ?? string.Empty;
#else
            var entryLocation = Assembly.GetEntryAssembly()?.Location ?? string.Empty;
#endif
            var exePath = Path.GetDirectoryName(entryLocation);
            if (string.IsNullOrEmpty(exePath))
            {
                throw new InvalidOperationException("Unable to determine executable path.");
            }
            var filePath = Path.Combine(exePath, fileName);
            File.WriteAllText(filePath, json);
        }
        else
        {
            Console.WriteLine(json);
        }
    }

    internal static void GenerateMultiResourceManifest(ResourceRegistry registry, bool save)
    {
        var manifests = registry.GetAll()
            .Select(r => BuildResourceManifest(r, true))
            .ToList();

        var wrapper = new MultiResourceManifest { Resources = manifests };
        var json = JsonSerializer.Serialize(wrapper, SourceGenerationContext.Default.MultiResourceManifest);

        if (save)
        {
#if NET6_0_OR_GREATER
            var entryLocation = Environment.ProcessPath ?? string.Empty;
#else
            var entryLocation = Assembly.GetEntryAssembly()?.Location ?? string.Empty;
#endif
            var fullExeName = Path.GetFileName(entryLocation);
            var exeName = Path.GetFileNameWithoutExtension(fullExeName);
            var fileName = $"{exeName}.dsc.manifests.json";
            var exePath = Path.GetDirectoryName(entryLocation);
            if (string.IsNullOrEmpty(exePath))
            {
                throw new InvalidOperationException("Unable to determine executable path.");
            }
            var filePath = Path.Combine(exePath, fileName);
            File.WriteAllText(filePath, json);
        }
        else
        {
            Console.WriteLine(json);
        }
    }

    private static DscResourceManifest BuildResourceManifest(ResourceRegistration registration, bool isMultiResourceExe)
    {
#if NET6_0_OR_GREATER
        var exePath = Environment.ProcessPath ?? string.Empty;
#else
        var exePath = Assembly.GetEntryAssembly()?.Location ?? string.Empty;
#endif
        var exeName = Path.GetFileName(exePath);
        var metadata = registration.Metadata;

        var manifest = new DscResourceManifest
        {
            Type = metadata.Type,
            Version = metadata.Version.ToString(),
            Description = !string.IsNullOrEmpty(metadata.Description) ? metadata.Description : null,
            Tags = metadata.Tags?.Length > 0 ? metadata.Tags : null,
            ExitCodes = BuildExitCodes(registration)
        };

        if (registration.GetAction is not null)
        {
            manifest.Get = BuildGetMethod(exeName, registration, isMultiResourceExe);
        }

        if (registration.SetAction is not null)
        {
            manifest.Set = BuildSetMethod(exeName, registration, isMultiResourceExe);
        }

        if (registration.TestAction is not null)
        {
            manifest.Test = BuildTestMethod(exeName, registration, isMultiResourceExe);
        }

        if (registration.DeleteAction is not null)
        {
            manifest.Delete = BuildDeleteMethod(exeName, registration, isMultiResourceExe);
        }

        if (registration.ExportAction is not null)
        {
            manifest.Export = BuildExportMethod(exeName, registration, isMultiResourceExe);
        }

        if (registration.SchemaFunc is not null)
        {
            var schemaJson = registration.SchemaFunc();
            using var schemaDoc = JsonDocument.Parse(schemaJson);
            manifest.EmbeddedSchema = new ManifestSchema
            {
                Embedded = schemaDoc.RootElement.Clone()
            };
        }

        return manifest;
    }

    private static ManifestMethod BuildGetMethod(string exeName, ResourceRegistration registration, bool isMultiResourceExe)
    {
        return BuildMethod(exeName, "get", registration, isMultiResourceExe);
    }

    private static ManifestSetMethod BuildSetMethod(string exeName, ResourceRegistration registration, bool isMultiResourceExe)
    {
        var metadata = registration.Metadata;
        string? setReturn = null;

        if (metadata.SetReturn != SetReturn.None)
        {
            var setReturnStr = metadata.SetReturn.ToString();
            setReturn = char.ToLowerInvariant(setReturnStr[0]) + setReturnStr.Substring(1);
        }

        var method = BuildMethod(exeName, "set", registration, isMultiResourceExe);
        return new ManifestSetMethod
        {
            Executable = method.Executable,
            Args = method.Args,
            Return = setReturn
        };
    }

    private static ManifestTestMethod BuildTestMethod(string exeName, ResourceRegistration registration, bool isMultiResourceExe)
    {
        var metadata = registration.Metadata;
        var testReturnStr = metadata.TestReturn.ToString();
        var testReturn = char.ToLowerInvariant(testReturnStr[0]) + testReturnStr.Substring(1);

        var method = BuildMethod(exeName, "test", registration, isMultiResourceExe);
        return new ManifestTestMethod
        {
            Executable = method.Executable,
            Args = method.Args,
            Return = testReturn
        };
    }

    private static ManifestMethod BuildDeleteMethod(string exeName, ResourceRegistration registration, bool isMultiResourceExe)
    {
        return BuildMethod(exeName, "delete", registration, isMultiResourceExe);
    }

    private static ManifestExportMethod BuildExportMethod(string exeName, ResourceRegistration registration, bool isMultiResourceExe)
    {
        if (isMultiResourceExe)
        {
            return new ManifestExportMethod
            {
                Executable = exeName,
                Args = ["export", "--resource", registration.Type]
            };
        }
        else
        {
            return new ManifestExportMethod
            {
                Executable = exeName,
                Args = ["export"]
            };
        }
    }

    private static ManifestMethod BuildMethod(string exeName, string verb, ResourceRegistration registration, bool isMultiResourceExe)
    {
        if (isMultiResourceExe)
        {
            return new ManifestMethod
            {
                Executable = exeName,
                Args = [verb, "--resource", registration.Type, new JsonInputArg { Arg = "--input", Mandatory = true }]
            };
        }
        else
        {
            return new ManifestMethod
            {
                Executable = exeName,
                Args = [verb, new JsonInputArg { Arg = "--input", Mandatory = true }]
            };
        }
    }

    private static Dictionary<string, string>? BuildExitCodes(ResourceRegistration registration)
    {
        var attrs = registration.ResourceType.GetCustomAttributes<ExitCodeAttribute>();

        if (attrs == null || !attrs.Any())
        {
            return null;
        }

        var exitCodes = new Dictionary<string, string>();

        foreach (var attr in attrs)
        {
            exitCodes[attr.ExitCode.ToString()] = attr.Description;
        }

        return exitCodes;
    }

    private static string GetSingleResourceManifestFileName(string resourceType)
    {
        // Convert "Owner/Name" to "owner.name.dsc.resource.json"
        return resourceType.ToLowerInvariant().Replace('/', '.') + ".dsc.resource.json";
    }
}

internal class MultiResourceManifest
{
    public List<DscResourceManifest> Resources { get; set; } = [];
}
