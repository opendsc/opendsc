// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.CommandLine;

namespace OpenDsc.Resource.CommandLine;

/// <summary>
/// Builder for creating command-line interfaces for DSC resources using System.CommandLine.
/// Supports both single and multi-resource executables.
/// </summary>
public sealed class CommandBuilder
{
    private readonly ResourceRegistry _registry = new();
    private bool IsSingleResource => _registry.Count == 1;

    private readonly Option<string> _requiredResourceOption = new("--resource", "-r")
    {
        Description = "Specify the DSC resource type",
        Required = true
    };

    private readonly Option<string> _optionalResourceOption = new("--resource", "-r")
    {
        Description = "Specify the DSC resource type"
    };

    /// <summary>
    /// Adds a DSC resource to the command line interface.
    /// Multiple resources can be registered to create a multi-resource executable.
    /// </summary>
    /// <typeparam name="TResource">The resource type that inherits from <see cref="DscResource{TSchema}"/>.</typeparam>
    /// <typeparam name="TSchema">The schema type that defines the resource's properties.</typeparam>
    /// <param name="resource">The resource instance to register.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when resource is null.</exception>
    public CommandBuilder AddResource<TResource, TSchema>(TResource resource)
        where TResource : DscResource<TSchema>
        where TSchema : class
    {
        if (resource == null)
        {
            throw new ArgumentNullException(nameof(resource));
        }

        _registry.Register<TResource, TSchema>(resource);
        return this;
    }

    /// <summary>
    /// Builds and returns the root command with all registered resources.
    /// Creates commands for get, set, test, delete, export, schema, and manifest operations.
    /// </summary>
    /// <returns>A configured <see cref="RootCommand"/> for the registered resources.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no resources have been registered.</exception>
    public RootCommand Build()
    {
        if (!_registry.HasResources)
        {
            throw new InvalidOperationException("No resources registered. Call AddResource() before Build().");
        }

        var rootCommand = new RootCommand("DSC Resource Command Line Interface");

        // Add verb commands
        rootCommand.Subcommands.Add(CreateGetCommand());
        rootCommand.Subcommands.Add(CreateSetCommand());
        rootCommand.Subcommands.Add(CreateTestCommand());
        rootCommand.Subcommands.Add(CreateDeleteCommand());
        rootCommand.Subcommands.Add(CreateExportCommand());
        rootCommand.Subcommands.Add(CreateSchemaCommand());
        rootCommand.Subcommands.Add(CreateManifestCommand());

        return rootCommand;
    }

    private Command CreateGetCommand()
    {
        var command = new Command("get", "Get the current state of a resource instance");

        var inputOption = new Option<string>("--input", "-i")
        {
            Description = "JSON input for the resource instance",
            Required = true
        };

        if (!IsSingleResource)
        {
            command.Options.Add(_requiredResourceOption);
        }
        command.Options.Add(inputOption);

        command.SetAction(parseResult =>
        {
            try
            {
                var resourceType = parseResult.GetValue(_requiredResourceOption);
                var input = parseResult.GetValue(inputOption)!;
                var registration = ResolveResource(resourceType, IsSingleResource);
                CommandExecutor.ExecuteGet(registration, input);
            }
            catch (Exception ex)
            {
                HandleException(ex, parseResult.GetValue(_requiredResourceOption));
            }
            return 0;
        });

        return command;
    }

    private Command CreateSetCommand()
    {
        var command = new Command("set", "Set the desired state of a resource instance");

        var inputOption = new Option<string>("--input", "-i")
        {
            Description = "JSON input for the desired state",
            Required = true
        };

        if (!IsSingleResource)
        {
            command.Options.Add(_requiredResourceOption);
        }
        command.Options.Add(inputOption);

        command.SetAction(parseResult =>
        {
            try
            {
                var resourceType = parseResult.GetValue(_requiredResourceOption);
                var input = parseResult.GetValue(inputOption)!;
                var registration = ResolveResource(resourceType, IsSingleResource);
                CommandExecutor.ExecuteSet(registration, input);
            }
            catch (Exception ex)
            {
                HandleException(ex, parseResult.GetValue(_requiredResourceOption));
            }
            return 0;
        });

        return command;
    }

    private Command CreateTestCommand()
    {
        var command = new Command("test", "Test if a resource instance is in the desired state");

        var inputOption = new Option<string>("--input", "-i")
        {
            Description = "JSON input for the desired state",
            Required = true
        };

        if (!IsSingleResource)
        {
            command.Options.Add(_requiredResourceOption);
        }
        command.Options.Add(inputOption);

        command.SetAction(parseResult =>
        {
            try
            {
                var resourceType = parseResult.GetValue(_requiredResourceOption);
                var input = parseResult.GetValue(inputOption)!;
                var registration = ResolveResource(resourceType, IsSingleResource);
                CommandExecutor.ExecuteTest(registration, input);
            }
            catch (Exception ex)
            {
                HandleException(ex, parseResult.GetValue(_requiredResourceOption));
            }
            return 0;
        });

        return command;
    }

    private Command CreateDeleteCommand()
    {
        var command = new Command("delete", "Delete a resource instance");

        var inputOption = new Option<string>("--input", "-i")
        {
            Description = "JSON input identifying the resource instance",
            Required = true
        };

        if (!IsSingleResource)
        {
            command.Options.Add(_requiredResourceOption);
        }
        command.Options.Add(inputOption);

        command.SetAction(parseResult =>
        {
            try
            {
                var resourceType = parseResult.GetValue(_requiredResourceOption);
                var input = parseResult.GetValue(inputOption)!;
                var registration = ResolveResource(resourceType, IsSingleResource);
                CommandExecutor.ExecuteDelete(registration, input);
            }
            catch (Exception ex)
            {
                HandleException(ex, parseResult.GetValue(_requiredResourceOption));
            }
            return 0;
        });

        return command;
    }

    private Command CreateExportCommand()
    {
        var command = new Command("export", "Export all instances of a resource");

        if (!IsSingleResource)
        {
            command.Options.Add(_requiredResourceOption);
        }

        command.SetAction(parseResult =>
        {
            try
            {
                var resourceType = parseResult.GetValue(_requiredResourceOption);
                var registration = ResolveResource(resourceType, IsSingleResource);
                CommandExecutor.ExecuteExport(registration);
            }
            catch (Exception ex)
            {
                HandleException(ex, parseResult.GetValue(_requiredResourceOption));
            }
            return 0;
        });

        return command;
    }

    private Command CreateSchemaCommand()
    {
        var command = new Command("schema", "Get the JSON schema for a resource");

        if (!IsSingleResource)
        {
            command.Options.Add(_requiredResourceOption);
        }

        command.SetAction(parseResult =>
        {
            try
            {
                var resourceType = parseResult.GetValue(_requiredResourceOption);
                var registration = ResolveResource(resourceType, IsSingleResource);
                CommandExecutor.ExecuteSchema(registration);
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex.Message);
                Logger.WriteTrace($"Exception: {ex.GetType().FullName}");
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    Logger.WriteTrace(ex.StackTrace);
                }
                Environment.Exit(1);
            }
            return 0;
        });

        return command;
    }

    private Command CreateManifestCommand()
    {
        var command = new Command("manifest", "Generate the DSC resource manifest(s)");

        var saveOption = new Option<bool>("--save", "-s")
        {
            Description = "Save the manifest to a file in the executable directory"
        };

        if (!IsSingleResource)
        {
            command.Options.Add(_optionalResourceOption!);
        }
        command.Options.Add(saveOption);

        command.SetAction(parseResult =>
        {
            try
            {
                var resourceType = parseResult.GetValue(_optionalResourceOption!);
                var save = parseResult.GetValue(saveOption);

                if (string.IsNullOrEmpty(resourceType) && !IsSingleResource)
                {
                    // Generate multi-resource manifest
                    CommandExecutor.GenerateMultiResourceManifest(_registry, save);
                }
                else
                {
                    // Generate single resource manifest
                    var registration = ResolveResource(resourceType, IsSingleResource);
                    CommandExecutor.GenerateSingleResourceManifest(registration, _registry.Count > 1, save);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteError(ex.Message);
                Logger.WriteTrace($"Exception: {ex.GetType().FullName}");
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    Logger.WriteTrace(ex.StackTrace);
                }
                Environment.Exit(1);
            }
            return 0;
        });

        return command;
    }

    private ResourceRegistration ResolveResource(string? resourceType, bool isSingleResource)
    {
        if (isSingleResource)
        {
            return _registry.GetAll()[0];
        }

        var registration = _registry.GetByType(resourceType ?? string.Empty);
        if (registration == null)
        {
            var available = string.Join(", ", _registry.GetAll().Select(r => r.Type));
            var message = $"Resource type '{resourceType}' not found. Available resources: {available}";
            // Write plain text to stdout and exit so callers reliably capture the message
            Console.WriteLine(message);
            Environment.Exit(1);
        }

        return registration!;
    }

    private void HandleException(Exception ex, string? resourceType)
    {
        Logger.WriteError(ex.Message);
        Logger.WriteTrace($"Exception: {ex.GetType().FullName}");

        if (!string.IsNullOrEmpty(ex.StackTrace))
        {
            Logger.WriteTrace(ex.StackTrace);
        }

        try
        {
            var registration = ResolveResource(resourceType, IsSingleResource);
            var exitCode = registration.ExitCodeResolver(ex.GetType());
            Environment.Exit(exitCode);
        }
        catch
        {
            // If resolution fails, use generic error code
            Environment.Exit(1);
        }
    }
}
