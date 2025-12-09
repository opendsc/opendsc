# OpenDsc AI Development Guide

## Project Overview

OpenDsc is a C# library ecosystem for building Microsoft DSC v3 resources with support for .NET Standard 2.0, .NET 8/9/10, and Native AOT compilation. The project consists of three packages:

- **OpenDsc.Resource**: Core DSC resource base classes and interfaces
- **OpenDsc.Resource.CommandLine**: CLI generation using System.CommandLine
- **OpenDsc.Templates**: dotnet new templates for scaffolding DSC resources

## Architecture Patterns

### DSC Resource Implementation

Resources inherit from `DscResource<T>` and implement capability interfaces:

```csharp
[DscResource("Owner/ResourceName", Description = "...", Tags = ["tag1"],
             SetReturn = SetReturn.StateAndDiff, TestReturn = TestReturn.StateAndDiff)]
public class Resource(JsonSerializerContext context) : DscResource<Schema>(context),
    IGettable<Schema>, ISettable<Schema>, ITestable<Schema>, IDeletable<Schema>
{
    public Schema Get(Schema instance) { /* return current state */ }
    public SetResult<Schema>? Set(Schema instance) { /* apply config, return changes */ }
    public TestResult<Schema> Test(Schema instance) { /* check drift */ }
    public void Delete(Schema instance) { /* remove resource */ }
}
```

### Command Line Interface

**Single Resource**: Register one resource in the executable:
```csharp
var resource = new Resource(SourceGenerationContext.Default);
var command = new CommandBuilder()
    .AddResource<Resource, Schema>(resource)
    .Build();
return command.Parse(args).Invoke();
```

**Multi-Resource**: Register multiple resources in a single executable:
```csharp
var fileResource = new FileResource(SourceGenerationContext.Default);
var userResource = new UserResource(SourceGenerationContext.Default);

var command = new CommandBuilder()
    .AddResource<FileResource, FileSchema>(fileResource)
    .AddResource<UserResource, UserSchema>(userResource)
    .Build();
return command.Parse(args).Invoke();
```

For multi-resource executables, commands require `--resource` parameter to specify the target resource.

### JSON Serialization Approaches

**Native AOT (preferred)**: Use source generation with `JsonSerializerContext`:
```csharp
[JsonSourceGenerationOptions(WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Schema))]
[JsonSerializable(typeof(TestResult<Schema>))]
[JsonSerializable(typeof(SetResult<Schema>))]
internal partial class SourceGenerationContext : JsonSerializerContext { }
```

**Non-AOT**: Use `JsonSerializerOptions` with `DscJsonSerializerSettings.Default`

### Result Types

- `SetResult<T>`: Contains `ActualState` and optional `ChangedProperties` (list of changed property names)
- `TestResult<T>`: Contains `ActualState` and optional `DifferingProperties` (list of properties not matching desired state)
- Both work with `SetReturn`/`TestReturn` enum attributes to control output verbosity

### Exit Code Mapping

Use `[ExitCode]` attributes to map exceptions to specific exit codes for CLI error handling:
```csharp
[ExitCode(0, Description = "Success")]
[ExitCode(1, Description = "Invalid parameter")]
[ExitCode(2, Exception = typeof(Exception), Description = "Generic error")]
```

## Build & Test Workflow

### Build Commands
```powershell
# Build all projects
.\build.ps1

# Build specific configuration
.\build.ps1 -Configuration Debug

# Skip tests
.\build.ps1 -SkipTest

# Pack for NuGet
dotnet pack src --configuration Release
```

### Test Execution

Tests use **Pester** (PowerShell testing framework) to verify DSC resources through the actual DSC CLI:
```powershell
# Run all tests (requires DSC v3 installed)
Invoke-Pester

# Tests validate: resource discovery, schema generation, get/set/test/delete operations
```

Test projects (`TestResource.Aot`, `TestResource.NonAot`, `TestResource.Options`, `TestResource.Multi`) are published during build, then tested via `dsc resource` commands. The `TestResource.Multi` project demonstrates multiple resources in a single executable.

## Project Conventions

### Multi-targeting Strategy
- Library projects target: `netstandard2.0;net8.0;net9.0;net10.0`
- Test/example projects target latest: `net10.0`
- AOT compatibility marked with `<IsAotCompatible>true</IsAotCompatible>` for .NET 8+

### Naming Conventions
- DSC resource types follow pattern: `Owner[.Group][.Area]/Name` (validated by regex in `DscResourceAttribute`)
- Assembly names use kebab-case for executables: `test-resource-aot`
- Schema properties use camelCase JSON serialization
- DSC-specific properties prefixed with underscore: `_exist`, `_inDesiredState`

### Project File Patterns

AOT-enabled resources require specific properties:
```xml
<PublishAot>true</PublishAot>
<InvariantGlobalization>true</InvariantGlobalization>
<IlcOptimizationPreference>Size</IlcOptimizationPreference>
```

Manifest generation post-publish target:
```xml
<Target Name="RunAfterPublish" AfterTargets="Publish">
  <Exec Command="$(PublishDir)$(AssemblyName).exe manifest --save" />
</Target>
```

This generates:
- Single resource: `owner.resourcename.dsc.resource.json`
- Multi-resource: `executable-name.dsc.manifests.json`

## Common Tasks

### Creating a New Resource
```powershell
dotnet new install OpenDsc.Templates
dotnet new dsc --aot true --resource-name "Owner/Resource" --resource-description "..."
```

### Testing Changes
After modifying library code, rebuild and test:
```powershell
.\build.ps1  # Builds src/, publishes test resources, runs Pester tests
```

### Version Management
- Update version in `.csproj` files (all three packages should stay in sync)
- Follow semantic versioning
- Update `CHANGELOG.md` following Keep a Changelog format

## Command Line Structure

### Command Hierarchy
Commands are at the root level (no `config` parent):
- `get`, `set`, `test`, `delete`, `export` - Resource operations
- `schema` - JSON schema output
- `manifest` - Manifest generation with optional `--save` flag

### Multi-Resource Support
When multiple resources are registered:
- All commands require `--resource Owner/Name` parameter
- `manifest` command can target all resources or a specific one
- Generates `executable.dsc.manifests.json` for multi-resource discovery

## Key Files Reference

- `src/OpenDsc.Resource/DscResource.cs`: Base class implementation
- `src/OpenDsc.Resource/Interfaces.cs`: Capability interfaces (IGettable, ISettable, etc.)
- `src/OpenDsc.Resource.CommandLine/CommandBuilder.cs`: CLI generation with builder pattern
- `src/OpenDsc.Resource.CommandLine/ResourceRegistry.cs`: Multi-resource registration
- `src/OpenDsc.Resource.CommandLine/CommandExecutor.cs`: Command execution logic
- `tests/TestResource.Aot/`: Reference implementation with AOT support
- `tests/TestResource.Multi/`: Multi-resource example (File, User, Service)
- `build.ps1`: Central build orchestration script
