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

Test projects (`TestResource.Aot`, `TestResource.NonAot`, `TestResource.Options`) are published during build, then tested via `dsc resource` commands.

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
  <Exec Command="$(PublishDir)$(TargetName).exe manifest &gt; $(PublishDir)$(OutputFileName)" />
</Target>
```

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

## Key Files Reference

- `src/OpenDsc.Resource/DscResource.cs`: Base class implementation
- `src/OpenDsc.Resource/Interfaces.cs`: Capability interfaces (IGettable, ISettable, etc.)
- `src/OpenDsc.Resource.CommandLine/CommandBuilder.cs`: CLI generation logic
- `tests/TestResource.Aot/`: Reference implementation with AOT support
- `build.ps1`: Central build orchestration script
