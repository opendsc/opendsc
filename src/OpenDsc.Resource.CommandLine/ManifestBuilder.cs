// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace OpenDsc.Resource.CommandLine;

public static class ManifestBuilder
{
    public static DscResourceManifest Build<TSchema>(IDscResource<TSchema> resource)
    {
        var dscAttr = resource.GetType().GetCustomAttribute<DscResourceAttribute>()
                      ?? throw new InvalidOperationException($"Resource does not have '{nameof(DscResourceAttribute)}' attribute.");

        var fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName)
            ?? throw new InvalidOperationException($"Unable to get current process file name.");

        var manifest = new DscResourceManifest
        {
            Schema = dscAttr.ManifestSchema,
            Type = dscAttr.Type,
            Version = dscAttr.Version.ToString(),
            Description = !string.IsNullOrEmpty(dscAttr.Description) ? dscAttr.Description : null,
            Tags = dscAttr.Tags.Length > 0 ? dscAttr.Tags : null,
            ExitCodes = BuildExitCodes(resource),
            EmbeddedSchema = BuildSchema(resource)
        };

        if (resource is IGettable<TSchema>)
        {
            manifest.Get = new ManifestMethod
            {
                Executable = fileName,
                Args = ["config", "get", new JsonInputArg { Arg = "--input", Mandatory = true }]
            };
        }

        if (resource is ISettable<TSchema>)
        {
            string? setReturn = null;
            if (dscAttr.SetReturn != SetReturn.None)
            {
                var setReturnStr = dscAttr.SetReturn.ToString();
                setReturn = char.ToLowerInvariant(setReturnStr[0]) + setReturnStr.Substring(1);
            }

            manifest.Set = new ManifestSetMethod
            {
                Executable = fileName,
                Args = ["config", "set", new JsonInputArg { Arg = "--input", Mandatory = true }],
                Return = setReturn
            };
        }

        if (resource is ITestable<TSchema>)
        {
            var testReturnStr = dscAttr.TestReturn.ToString();
            var testReturn = char.ToLowerInvariant(testReturnStr[0]) + testReturnStr.Substring(1);

            manifest.Test = new ManifestTestMethod
            {
                Executable = fileName,
                Args = ["config", "test", new JsonInputArg { Arg = "--input", Mandatory = true }],
                Return = testReturn
            };
        }

        if (resource is IDeletable<TSchema>)
        {
            manifest.Delete = new ManifestMethod
            {
                Executable = fileName,
                Args = ["config", "delete", new JsonInputArg { Arg = "--input", Mandatory = true }]
            };
        }

        if (resource is IExportable<TSchema>)
        {
            manifest.Export = new ManifestExportMethod
            {
                Executable = fileName,
                Args = ["config", "export"]
            };
        }

        return manifest;
    }

    private static Dictionary<string, string>? BuildExitCodes<TSchema>(IDscResource<TSchema> resource)
    {
        var attrs = resource.GetType().GetCustomAttributes<ExitCodeAttribute>();

        if (attrs is null || !attrs.Any())
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

    private static ManifestSchema BuildSchema<TSchema>(IDscResource<TSchema> resource)
    {
        using var schemaDoc = JsonDocument.Parse(resource.GetSchema());
        return new ManifestSchema
        {
            Embedded = schemaDoc.RootElement.Clone()
        };
    }
}
