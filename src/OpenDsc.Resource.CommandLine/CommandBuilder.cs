// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.CommandLine;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

#if NET6_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace OpenDsc.Resource.CommandLine;

public static class CommandBuilder<TResource, TSchema> where TResource : IDscResource<TSchema>
{
#if NET6_0_OR_GREATER
    [RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
    public static RootCommand Build(TResource resource, JsonSerializerOptions options)
    {
        var inputOption = new Option<string>("--input", "The file JSON input.");
        inputOption.AddAlias("-i");
        inputOption.IsRequired = true;

        var whatIfOption = new Option<bool>("--what-if", "Preview changes without applying them.");
        whatIfOption.AddAlias("-w");

        var configCommand = new Command("config", "Manage resource.");

        if (resource is IGettable<TSchema>)
        {
            BuildGetCommand(resource, inputOption, configCommand);
        }

        if (resource is ISettable<TSchema>)
        {
            BuildSetCommand(resource, options, inputOption, whatIfOption, configCommand);
        }

        if (resource is ITestable<TSchema>)
        {
            BuildTestCommand(resource, options, inputOption, configCommand);
        }

        if (resource is IDeletable<TSchema>)
        {
            BuildDeleteCommand(resource, inputOption, whatIfOption, configCommand);
        }

        if (resource is IExportable<TSchema>)
        {
            BuildExportCommand(resource, configCommand);
        }

        var schemaCommand = BuildSchemaCommand(resource);
        var manifestCommand = BuildManifestCommand(resource, options);

        var rootCommand = new RootCommand("Manage resource.");
        rootCommand.AddCommand(configCommand);
        rootCommand.AddCommand(schemaCommand);
        rootCommand.AddCommand(manifestCommand);

        return rootCommand;
    }

    public static RootCommand Build(TResource resource, JsonSerializerContext context)
    {
        var inputOption = new Option<string>("--input", "The file JSON input.");
        inputOption.AddAlias("-i");
        inputOption.IsRequired = true;

        var whatIfOption = new Option<bool>("--what-if", "Preview changes without applying them.");
        whatIfOption.AddAlias("-w");

        var configCommand = new Command("config", "Manage resource.");

        if (resource is IGettable<TSchema>)
        {
            BuildGetCommand(resource, inputOption, configCommand);
        }

        if (resource is ISettable<TSchema>)
        {
            BuildSetCommand(resource, context, inputOption, whatIfOption, configCommand);
        }

        if (resource is ITestable<TSchema>)
        {
            BuildTestCommand(resource, context, inputOption, configCommand);
        }

        if (resource is IDeletable<TSchema>)
        {
            BuildDeleteCommand(resource, inputOption, whatIfOption, configCommand);
        }

        if (resource is IExportable<TSchema>)
        {
            BuildExportCommand(resource, configCommand);
        }

        var schemaCommand = BuildSchemaCommand(resource);
        var manifestCommand = BuildManifestCommand(resource, context);

        var rootCommand = new RootCommand("Manage resource.");
        rootCommand.AddCommand(configCommand);
        rootCommand.AddCommand(schemaCommand);
        rootCommand.AddCommand(manifestCommand);

        return rootCommand;
    }

    private static void BuildGetCommand(TResource resource, Option<string> inputOption, Command configCommand)
    {
        var getCommand = new Command("get", "Retrieve resource configuration.")
            {
                inputOption
            };

        getCommand.SetHandler((string inputOption) =>
        {
            try
            {
                GetHandler(resource, inputOption);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
        }, inputOption);

        configCommand.AddCommand(getCommand);
    }

#if NET6_0_OR_GREATER
    [RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
    private static void BuildSetCommand(TResource resource, JsonSerializerOptions options, Option<string> inputOption, Option<bool> whatIfOption, Command configCommand)
    {
        var setCommand = new Command("set", "Set resource configuration.")
            {
                inputOption,
                whatIfOption
            };

        setCommand.SetHandler((string inputOption, bool whatIf) =>
        {
            try
            {
                SetHandler(resource, inputOption, options, whatIf);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
        }, inputOption, whatIfOption);

        configCommand.AddCommand(setCommand);
    }

    private static void BuildSetCommand(TResource resource, JsonSerializerContext context, Option<string> inputOption, Option<bool> whatIfOption, Command configCommand)
    {
        var setCommand = new Command("set", "Set resource configuration.")
            {
                inputOption,
                whatIfOption
            };

        setCommand.SetHandler((string inputOption, bool whatIf) =>
        {
            try
            {
                SetHandler(resource, inputOption, context, whatIf);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
        }, inputOption, whatIfOption);

        configCommand.AddCommand(setCommand);
    }

#if NET6_0_OR_GREATER
    [RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
    private static void BuildTestCommand(TResource resource, JsonSerializerOptions options, Option<string> inputOption, Command configCommand)
    {
        var testCommand = new Command("test", "Test resource configuration.")
            {
                inputOption
            };

        testCommand.SetHandler((string inputOption) =>
        {
            try
            {
                TestHandler(resource, inputOption, options);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
        }, inputOption);

        configCommand.AddCommand(testCommand);
    }

    private static void BuildTestCommand(TResource resource, JsonSerializerContext context, Option<string> inputOption, Command configCommand)
    {
        var testCommand = new Command("test", "Test resource configuration.")
            {
                inputOption
            };

        testCommand.SetHandler((string inputOption) =>
        {
            try
            {
                TestHandler(resource, inputOption, context);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
        }, inputOption);

        configCommand.AddCommand(testCommand);
    }

    private static void BuildDeleteCommand(TResource resource, Option<string> inputOption, Option<bool> whatIfOption, Command configCommand)
    {
        var deleteCommand = new Command("delete", "Delete resource configuration.")
            {
                inputOption,
                whatIfOption
            };

        deleteCommand.SetHandler((string inputOption, bool whatIf) =>
        {
            try
            {
                DeleteHandler(resource, inputOption, whatIf);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
        }, inputOption, whatIfOption);

        configCommand.AddCommand(deleteCommand);
    }

    private static void BuildExportCommand(TResource resource, Command configCommand)
    {
        var exportCommand = new Command("export", "Export resource configuration.");

        exportCommand.SetHandler(() =>
        {
            try
            {
                ExportHandler(resource);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
        });

        configCommand.AddCommand(exportCommand);
    }

    private static Command BuildSchemaCommand(TResource resource)
    {
        var schemaCommand = new Command("schema", "Retrieve resource JSON schema.");
        schemaCommand.SetHandler(() =>
        {
            try
            {
                SchemaHandler(resource);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
        });
        return schemaCommand;
    }

#if NET6_0_OR_GREATER
    [RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
    private static Command BuildManifestCommand(TResource resource, JsonSerializerOptions options)
    {
        var manifestCommand = new Command("manifest", "Retrieve resource manifest.");
        manifestCommand.SetHandler(() =>
        {
            try
            {
                ManifestHandler(resource, options);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
        });
        return manifestCommand;
    }

    private static Command BuildManifestCommand(TResource resource, JsonSerializerContext context)
    {
        var manifestCommand = new Command("manifest", "Retrieve resource manifest.");
        manifestCommand.SetHandler(() =>
        {
            try
            {
                ManifestHandler(resource, context);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
        });
        return manifestCommand;
    }

    private static void SchemaHandler(IDscResource<TSchema> resource)
    {
        Console.WriteLine(resource.GetSchema());
    }

#if NET6_0_OR_GREATER
    [RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
    private static void ManifestHandler(IDscResource<TSchema> resource, JsonSerializerOptions options)
    {
        options.Converters.Add(new ResourceConverter<TSchema>());

        var json = JsonSerializer.Serialize(resource, typeof(IDscResource<TSchema>), options);
        Console.WriteLine(json);
    }

    private static void ManifestHandler(IDscResource<TSchema> resource, JsonSerializerContext context)
    {
        var json = JsonSerializer.Serialize(resource, typeof(IDscResource<TSchema>), context);
        Console.WriteLine(json);
    }

    private static void GetHandler(IDscResource<TSchema> resource, string inputOption)
    {
        var instance = resource.Parse(inputOption);

        if (resource is not IGettable<TSchema> iGettable)
        {
            throw new NotImplementedException("Resource does not support Get capability.");
        }

        var result = iGettable.Get(instance);
        Console.WriteLine(resource.ToJson(result));
    }

#if NET6_0_OR_GREATER
    [RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
    private static void SetHandler(IDscResource<TSchema> resource, string inputOption, JsonSerializerOptions options, bool whatIf = false)
    {
        var instance = resource.Parse(inputOption);

        if (resource is not ISettable<TSchema> iSettable)
        {
            throw new NotImplementedException("Resource does not support Set capability.");
        }

        if (whatIf)
        {
            // TODO: Implement what-if logic for Set operation
        }

        var result = iSettable.Set(instance);
        var dscAttr = GetDscAttribute(resource);

        if (result is not null && dscAttr.SetReturn != SetReturn.None)
        {
            var json = resource.ToJson(result.ActualState);
            Console.WriteLine(json);
        }

        if (result?.ChangedProperties is not null && dscAttr.SetReturn == SetReturn.StateAndDiff)
        {
            var json = JsonSerializer.Serialize(result.ChangedProperties, options);
            Console.WriteLine(json);
        }
    }

    private static void SetHandler(IDscResource<TSchema> resource, string inputOption, JsonSerializerContext context, bool whatIf = false)
    {
        var instance = resource.Parse(inputOption);

        if (resource is not ISettable<TSchema> iSettable)
        {
            throw new NotImplementedException("Resource does not support Set capability.");
        }

        if (whatIf)
        {
            // TODO: Implement what-if logic for Set operation
        }

        var result = iSettable.Set(instance);
        var dscAttr = GetDscAttribute(resource);

        if (result is not null && dscAttr.SetReturn != SetReturn.None)
        {
            var json = resource.ToJson(result.ActualState);
            Console.WriteLine(json);
        }

        if (result?.ChangedProperties is not null && dscAttr.SetReturn == SetReturn.StateAndDiff)
        {
            var json = JsonSerializer.Serialize(result.ChangedProperties, typeof(HashSet<string>), context);
            Console.WriteLine(json);
        }
    }

#if NET6_0_OR_GREATER
    [RequiresDynamicCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCodeAttribute("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
#endif
    private static void TestHandler(IDscResource<TSchema> resource, string inputOption, JsonSerializerOptions options)
    {
        var instance = resource.Parse(inputOption);

        if (resource is not ITestable<TSchema> iTestable)
        {
            throw new NotImplementedException("Resource does not support Test capability.");
        }

        var result = iTestable.Test(instance);
        var json = JsonSerializer.Serialize(result.ActualState, options);
        Console.WriteLine(json);

        var dscAttr = GetDscAttribute(resource);

        if (result?.DifferingProperties is not null && dscAttr.TestReturn == TestReturn.StateAndDiff)
        {
            json = JsonSerializer.Serialize(result.DifferingProperties, options);
            Console.WriteLine(json);
        }
    }

    private static void TestHandler(IDscResource<TSchema> resource, string inputOption, JsonSerializerContext context)
    {
        var instance = resource.Parse(inputOption);

        if (resource is not ITestable<TSchema> iTestable)
        {
            throw new NotImplementedException("Resource does not support Test capability.");
        }

        var result = iTestable.Test(instance);
        var json = JsonSerializer.Serialize(result.ActualState, typeof(TSchema), context);
        Console.WriteLine(json);

        var dscAttr = GetDscAttribute(resource);

        if (result?.DifferingProperties is not null && dscAttr.TestReturn == TestReturn.StateAndDiff)
        {
            json = JsonSerializer.Serialize(result.DifferingProperties, typeof(HashSet<string>), context);
            Console.WriteLine(json);
        }
    }

    private static void DeleteHandler(IDscResource<TSchema> resource, string inputOption, bool whatIf = false)
    {
        var instance = resource.Parse(inputOption);

        if (resource is not IDeletable<TSchema> iTDeletable)
        {
            throw new NotImplementedException("Resource does not support Delete capability.");
        }

        if (whatIf)
        {
            // TODO: Implement what-if logic for Delete operation
        }

        iTDeletable.Delete(instance);
    }

    private static void ExportHandler(IDscResource<TSchema> resource)
    {
        if (resource is not IExportable<TSchema> iExportable)
        {
            throw new NotImplementedException("Resource does not support Export capability.");
        }

        foreach (var instance in iExportable.Export())
        {
            var json = resource.ToJson(instance);
            Console.WriteLine(json);
        }
    }

    private static void HandleException(TResource resource, Exception e)
    {
        Logger.WriteError(e.Message);
        Logger.WriteTrace($"Exception: {e.GetType().FullName}");

        if (!string.IsNullOrEmpty(e.StackTrace))
        {
            Logger.WriteTrace(e.StackTrace);
        }

        try
        {
            var exitCode = ExitCodeResolver.GetExitCode(resource, e.GetType());
            Environment.Exit(exitCode);
        }
        catch
        {
            Environment.Exit(int.MaxValue);
        }
    }

    private static DscResourceAttribute GetDscAttribute(IDscResource<TSchema> resource)
    {
        return resource.GetType().GetCustomAttribute<DscResourceAttribute>()
                      ?? throw new InvalidOperationException($"Resource does not have '{nameof(DscResourceAttribute)}' attribute.");
    }
}