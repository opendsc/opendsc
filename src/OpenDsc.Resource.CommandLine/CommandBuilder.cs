// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.CommandLine;
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
        var serializer = new OptionsSerializer<TSchema>(options);
        return Build(resource, serializer);
    }

    public static RootCommand Build(TResource resource, JsonSerializerContext context)
    {
        var serializer = new ContextSerializer<TSchema>(context);
        return Build(resource, serializer);
    }

    private static RootCommand Build(TResource resource, ISerializer<TSchema> serializer)
    {
        var inputOption = new Option<string>("--input", "-i")
        {
            Description = CommandDescriptions.InputOption,
            Required = true
        };

        var configCommand = new Command("config", CommandDescriptions.Config);

        if (resource is IGettable<TSchema>)
        {
            BuildGetCommand(resource, inputOption, configCommand);
        }

        if (resource is ISettable<TSchema>)
        {
            BuildSetCommand(resource, serializer, inputOption, configCommand);
        }

        if (resource is ITestable<TSchema>)
        {
            BuildTestCommand(resource, serializer, inputOption, configCommand);
        }

        if (resource is IDeletable<TSchema>)
        {
            BuildDeleteCommand(resource, inputOption, configCommand);
        }

        if (resource is IExportable<TSchema>)
        {
            BuildExportCommand(resource, configCommand);
        }

        var schemaCommand = BuildSchemaCommand(resource);
        var manifestCommand = BuildManifestCommand(resource, serializer);

        var rootCommand = new RootCommand(CommandDescriptions.Root);
        rootCommand.Subcommands.Add(configCommand);
        rootCommand.Subcommands.Add(schemaCommand);
        rootCommand.Subcommands.Add(manifestCommand);

        return rootCommand;
    }

    private static void BuildGetCommand(TResource resource, Option<string> inputOption, Command configCommand)
    {
        var getCommand = new Command("get", CommandDescriptions.Get)
            {
                inputOption
            };

        getCommand.SetAction(parseResult =>
        {
            try
            {
                var input = parseResult.GetValue(inputOption)!;
                CommandHandlers<TResource, TSchema>.GetHandler(resource, input);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
            return 0;
        });

        configCommand.Subcommands.Add(getCommand);
    }

    private static void BuildSetCommand(TResource resource, ISerializer<TSchema> serializer, Option<string> inputOption, Command configCommand)
    {
        var setCommand = new Command("set", CommandDescriptions.Set)
            {
                inputOption
            };

        setCommand.SetAction(parseResult =>
        {
            try
            {
                var input = parseResult.GetValue(inputOption)!;
                CommandHandlers<TResource, TSchema>.SetHandler(resource, input, serializer);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
            return 0;
        });

        configCommand.Subcommands.Add(setCommand);
    }

    private static void BuildTestCommand(TResource resource, ISerializer<TSchema> serializer, Option<string> inputOption, Command configCommand)
    {
        var testCommand = new Command("test", CommandDescriptions.Test)
            {
                inputOption
            };

        testCommand.SetAction(parseResult =>
        {
            try
            {
                var input = parseResult.GetValue(inputOption)!;
                CommandHandlers<TResource, TSchema>.TestHandler(resource, input, serializer);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
            return 0;
        });

        configCommand.Subcommands.Add(testCommand);
    }

    private static void BuildDeleteCommand(TResource resource, Option<string> inputOption, Command configCommand)
    {
        var deleteCommand = new Command("delete", CommandDescriptions.Delete)
            {
                inputOption
            };

        deleteCommand.SetAction(parseResult =>
        {
            try
            {
                var input = parseResult.GetValue(inputOption)!;
                CommandHandlers<TResource, TSchema>.DeleteHandler(resource, input);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
            return 0;
        });

        configCommand.Subcommands.Add(deleteCommand);
    }

    private static void BuildExportCommand(TResource resource, Command configCommand)
    {
        var exportCommand = new Command("export", CommandDescriptions.Export);

        exportCommand.SetAction(parseResult =>
        {
            try
            {
                CommandHandlers<TResource, TSchema>.ExportHandler(resource);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
            return 0;
        });

        configCommand.Subcommands.Add(exportCommand);
    }

    private static Command BuildSchemaCommand(TResource resource)
    {
        var schemaCommand = new Command("schema", CommandDescriptions.Schema);
        schemaCommand.SetAction(parseResult =>
        {
            try
            {
                CommandHandlers<TResource, TSchema>.SchemaHandler(resource);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
            return 0;
        });
        return schemaCommand;
    }

    private static Command BuildManifestCommand(TResource resource, ISerializer<TSchema> serializer)
    {
        var manifestCommand = new Command("manifest", CommandDescriptions.Manifest);
        manifestCommand.SetAction(parseResult =>
        {
            try
            {
                CommandHandlers<TResource, TSchema>.ManifestHandler(resource, serializer);
            }
            catch (Exception e)
            {
                HandleException(resource, e);
            }
            return 0;
        });
        return manifestCommand;
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
}
