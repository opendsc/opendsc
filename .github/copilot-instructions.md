# OpenDSC - AI Coding Agent Instructions

## Project Overview

This repository contains Microsoft DSC (Desired State Configuration) resources for Windows and cross-platform management, plus a Local Configuration Manager (LCM) service. The project uses a **multi-resource executable architecture** where all resources are bundled into platform-specific executables (`OpenDsc.Resource.CommandLine.Windows.exe`, `.Linux`, and `.macOS`) that implement the standard DSC interface through the `OpenDsc.Resource.CommandLine` library.

**Major Components:**

1. **DSC Resources** - Built-in resources for system management
2. **LCM Service** (`src/OpenDsc.Lcm/`) - Background service that monitors and remediates configurations
3. **Resource Libraries** (`src/OpenDsc.Resource*`) - Core framework for building DSC resources

**Available Resources:**

Windows Resources (in `src/OpenDsc.Resource.Windows/`):
- `Environment/` - Environment variable management (`OpenDsc.Windows/Environment`)
- `Service/` - Windows service control (`OpenDsc.Windows/Service`)
- `Shortcut/` - Shortcut (.lnk) file management (`OpenDsc.Windows/Shortcut`)
- `Group/` - Local Windows group management (`OpenDsc.Windows/Group`)
- `User/` - Local Windows user accounts (`OpenDsc.Windows/User`)
- `OptionalFeature/` - Windows optional features via DISM (`OpenDsc.Windows/OptionalFeature`)
- `ScheduledTask/` - Scheduled task management (`OpenDsc.Windows/ScheduledTask`)
- `FileSystem/Acl/` - File system ACL management (`OpenDsc.Windows.FileSystem/AccessControlList`)

Cross-Platform Resources (in `src/OpenDsc.Resource.FileSystem/`, `src/OpenDsc.Resource.Xml/`, `src/OpenDsc.Resource.Archive/`, and `src/OpenDsc.Resource.Posix/`):
- `File/` - Cross-platform file management (`OpenDsc.FileSystem/File`)
- `Directory/` - Cross-platform directory management (`OpenDsc.FileSystem/Directory`)
- `SymbolicLink/` - Cross-platform symbolic link management (`OpenDsc.FileSystem/SymbolicLink`)
- `Element/` - XML element manipulation (`OpenDsc.Xml/Element`)
- `Zip/Compress/` - Create ZIP archives (`OpenDsc.Archive.Zip/Compress`)
- `Zip/Expand/` - Extract ZIP archives (`OpenDsc.Archive.Zip/Expand`)
- `FileSystem/Permission/` - POSIX file permissions (`OpenDsc.Posix.FileSystem/Permission`)

**Resource Naming Convention:**
- Windows-specific: `OpenDsc.Windows/<Name>` (namespace: `OpenDsc.Resource.Windows.<Name>`)
- Cross-platform: `OpenDsc.<Area>/<Name>` (namespace: `OpenDsc.Resource.<Area>.<Name>`)
- Specialized: `OpenDsc.Xml/<Name>`, `OpenDsc.Archive.Zip/<Name>` (namespace: `OpenDsc.Resource.Xml.<Name>`, `OpenDsc.Resource.Archive.Zip.<Name>`)
- **With sub-area**: `OpenDsc.Windows.<SubArea>/<Name>` (namespace: `OpenDsc.Resource.Windows.<SubArea>.<Name>`)
  - Example: `OpenDsc.Windows.FileSystem/AccessControlList` → folder `src/OpenDsc.Resource.Windows/FileSystem/Acl/` → namespace `OpenDsc.Resource.Windows.FileSystem.Acl`
  - Example: `OpenDsc.Archive.Zip/Compress` → folder `src/OpenDsc.Resource.Archive/Zip/Compress/` → namespace `OpenDsc.Resource.Archive.Zip.Compress`

## Architecture Pattern

### Multi-Resource Executable Structure

The project uses a **consolidated executable approach** where multiple resources are bundled into platform-specific executables:

**Platform Executables:**

The project uses a single unified executable (`src/OpenDsc.Resources/`) that contains all resources and conditionally compiles platform-specific resources based on the target OS:
- Windows builds include: All Windows resources + cross-platform resources
- Linux/macOS builds include: Cross-platform resources + POSIX resources only

This is achieved through conditional compilation (`#if WINDOWS` / `#if !WINDOWS`) in `Program.cs`.

**Resource Implementation Structure:**

Each resource is a folder within a shared project (e.g., `src/OpenDsc.Resource.Windows/Environment/`):

```
src/OpenDsc.Resource.Windows/
├── OpenDsc.Resource.Windows.csproj    # Shared project for all Windows resources
├── SourceGenerationContext.cs         # Shared JSON serialization context
├── Environment/
│   ├── Resource.cs                    # Core logic with [DscResource] attribute
│   ├── Schema.cs                      # JSON schema model
│   └── Scope.cs                       # Supporting types (if needed)
├── Group/
│   ├── Resource.cs
│   └── Schema.cs
└── ...other resources...

src/OpenDsc.Resources/
├── Program.cs                          # Entry point: registers all resources
└── OpenDsc.Resources.csproj

tests/
├── Environment.Tests.ps1               # Integration tests per resource
├── Group.Tests.ps1
└── ...other tests...
```

### Critical Inheritance Pattern

All resources inherit from `DscResource<Schema>` and implement capability interfaces:

```csharp
// Resource.cs in each resource folder (e.g., src/OpenDsc.Resource.Windows/Environment/Resource.cs)
[DscResource("OpenDsc.Windows/Environment", "0.1.0", Description = "Manage Windows environment variables", Tags = ["windows", "environment"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
public sealed class Resource(JsonSerializerContext context)
    : DscResource<Schema>(context),
      IGettable<Schema>,      // Read current state (optional, but recommended)
      ISettable<Schema>,      // Apply desired state (optional, but recommended)
      IDeletable<Schema>,     // Remove resource (optional, but recommended)
      IExportable<Schema>     // Export all instances (optional)
{
    public override string GetSchema() { /* ... */ }
    public Schema Get(Schema instance) { /* ... */ }
    public SetResult<Schema>? Set(Schema instance) { /* ... */ }
    public void Delete(Schema instance) { /* ... */ }
    public IEnumerable<Schema> Export() { /* ... */ }
}
```

**Platform Executable Entry Point (Program.cs):**

The `Program.cs` in `src/OpenDsc.Resources/` registers all resources using `CommandBuilder` with conditional compilation:

```csharp
// src/OpenDsc.Resources/Program.cs
using OpenDsc.Resource.CommandLine;

#if WINDOWS
using GroupNs = OpenDsc.Resource.Windows.Group;
using EnvironmentNs = OpenDsc.Resource.Windows.Environment;
using ScheduledTaskNs = OpenDsc.Resource.Windows.ScheduledTask;
using FileSystemAclNs = OpenDsc.Resource.Windows.FileSystem.Acl;  // Sub-area resource
#endif

#if !WINDOWS
using PosixPermissionNs = OpenDsc.Resource.Posix.FileSystem.Permission;
#endif

using ZipCompressNs = OpenDsc.Resource.Archive.Zip.Compress;
using ZipExpandNs = OpenDsc.Resource.Archive.Zip.Expand;
// ... other cross-platform resource namespaces

#if WINDOWS
var groupResource = new GroupNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var environmentResource = new EnvironmentNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var scheduledTaskResource = new ScheduledTaskNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
var fileSystemAclResource = new FileSystemAclNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
#endif

#if !WINDOWS
PosixPermissionNs.Resource? posixPermissionResource = null;
if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
{
    posixPermissionResource = new PosixPermissionNs.Resource(OpenDsc.Resource.Posix.SourceGenerationContext.Default);
}
#endif

var zipCompressResource = new ZipCompressNs.Resource(OpenDsc.Resource.Archive.SourceGenerationContext.Default);
var zipExpandResource = new ZipExpandNs.Resource(OpenDsc.Resource.Archive.SourceGenerationContext.Default);
// ... instantiate other cross-platform resources

var command = new CommandBuilder();

#if WINDOWS
command
    .AddResource<GroupNs.Resource, GroupNs.Schema>(groupResource)
    .AddResource<EnvironmentNs.Resource, EnvironmentNs.Schema>(environmentResource)
    .AddResource<ScheduledTaskNs.Resource, ScheduledTaskNs.Schema>(scheduledTaskResource)
    .AddResource<FileSystemAclNs.Resource, FileSystemAclNs.Schema>(fileSystemAclResource);
#endif

#if !WINDOWS
if (posixPermissionResource is not null)
{
    command.AddResource<PosixPermissionNs.Resource, PosixPermissionNs.Schema>(posixPermissionResource);
}
#endif

command
    .AddResource<ZipCompressNs.Resource, ZipCompressNs.Schema>(zipCompressResource)
    .AddResource<ZipExpandNs.Resource, ZipExpandNs.Schema>(zipExpandResource);

return command.Build().Parse(args).Invoke();
```

**Shared SourceGenerationContext:**

Resources within the same project share a `SourceGenerationContext.cs` at the project root:

```csharp
// src/OpenDsc.Resource.Windows/SourceGenerationContext.cs
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Group.Schema), TypeInfoPropertyName = "GroupSchema")]
[JsonSerializable(typeof(User.Schema), TypeInfoPropertyName = "UserSchema")]
[JsonSerializable(typeof(Environment.Schema), TypeInfoPropertyName = "EnvironmentSchema")]
// ... other schemas
public partial class SourceGenerationContext : JsonSerializerContext { }
```

**GetSchema() Method:**

All resources must override `GetSchema()` to generate JSON schema. Use this exact pattern - it's standardized across all resources:

```csharp
public override string GetSchema()
{
    var config = new SchemaGeneratorConfiguration()
    {
        PropertyNameResolver = PropertyNameResolvers.CamelCase
    };

    var builder = new JsonSchemaBuilder().FromType<Schema>(config);
    builder.Schema("https://json-schema.org/draft/2020-12/schema");
    var schema = builder.Build();

    return JsonSerializer.Serialize(schema);
}
```

**Alternative: Embedded Schema (for complex schemas):**

For resources with very complex schemas (e.g., ScheduledTask with nested objects), you can use a pre-generated embedded JSON schema file:

```csharp
public override string GetSchema()
{
    var assembly = typeof(Resource).Assembly;
    var resourceName = "OpenDsc.Resource.Windows.ScheduledTask.schema.json";

    using var stream = assembly.GetManifestResourceStream(resourceName)
        ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
}
```

The embedded schema file must be added to the `.csproj` as an `EmbeddedResource`:
```xml
<ItemGroup>
  <EmbeddedResource Include="ScheduledTask\schema.json" />
</ItemGroup>
```

**Interface Contract (all interfaces are optional, implement those that make sense):**
- `IGettable.Get(Schema)` → return current state
  - Set `Exist = false` if resource not found (explicitly set to false)
  - Do NOT set `Exist = true` if resource exists (true is the default value, omit the property)
- `ISettable.Set(Schema)` → apply changes, return `null` or `SetResult<Schema>`
  - **Important:** Do NOT check `_exist` in `Set()` - the DSC engine calls `Set()` when `_exist=true` and `Delete()` when `_exist=false`
- `IDeletable.Delete(Schema)` → remove resource
- `IExportable.Export()` → yield all instances

**Best Practice:** Strive to implement all interfaces that make sense for your resource type.

### Schema Conventions

Schemas use JSON Schema Generation attributes from `Json.Schema.Generation`:

```csharp
[Title("...")]
[Description("...")]
[AdditionalProperties(false)]
public sealed class Schema
{
    [Required]
    [Pattern(@"regex")]
    public string Name { get; set; }

    [WriteOnly]  // Property accepted in Set but never returned by Get
    [Nullable(false)]
    public string? Password { get; set; }

    [JsonPropertyName("_exist")]  // DSC canonical properties prefixed with _
    [Default(true)]
    public bool? Exist { get; set; }
}
```

**Naming Rules:**
- User-facing properties: camelCase (enforced by `PropertyNameResolver.CamelCase`)
- DSC canonical properties: `_exist`, `_purge`, `_inDesiredState` (underscore prefix)
- Enums: PascalCase values (e.g., `User`, `Machine`)

**DSC Canonical Properties:**

DSC defines several canonical properties that provide shared semantics across resources. These properties always start with an underscore (`_`) and should not be overridden or extended:

- **`_exist` (bool?)** - Indicates whether the resource instance should exist. Used to enforce create/update/delete operations during `set`. This is the most commonly used canonical property.
  - Default: `true`
  - Use in: Resources that manage instance lifecycle

- **`_purge` (bool?)** - Write-only property for list-based resources to indicate whether unmanaged entries should be removed. Useful for resources managing collections where you want to either allow or remove items not explicitly defined.
  - Default: `false` (additive mode - only add specified items)
  - When `true`: Exact mode - removes items not in the specified list
  - When `false`: Additive mode - only adds items from the list without removing others
  - Use in: Resources managing lists/collections (e.g., group members, installed features)
  - Mark with `[WriteOnly]` attribute since it's a control property, not state
  - Example: `Group` resource uses `_purge` to control group membership behavior

- **`_inDesiredState` (bool?)** - Read-only property indicating whether the instance is in desired state. Mandatory for resources that implement the `test` operation.
  - Use in: Resources implementing `ITestable<Schema>`

**`_purge` Implementation Pattern:**

For resources managing collections (e.g., group members, list items), implement the `_purge` pattern:

```csharp
// Schema.cs
[Description("List of items in the collection.")]
[Nullable(false)]
public string[]? Items { get; set; }

[JsonPropertyName("_purge")]
[Description("When true, removes items not in the Items list. When false, only adds items from the Items list without removing others.")]
[Nullable(false)]
[WriteOnly]
[Default(false)]
public bool? Purge { get; set; }

// Resource.cs - Set() implementation
if (instance.Items != null)
{
    var currentItems = new HashSet<string>(GetCurrentItems(resource), StringComparer.OrdinalIgnoreCase);
    var desiredItems = new HashSet<string>(instance.Items, StringComparer.OrdinalIgnoreCase);

    // When _purge is true, remove items not in desired list (exact mode)
    if (instance.Purge == true)
    {
        var toRemove = currentItems.Except(desiredItems).ToList();
        foreach (var item in toRemove)
        {
            RemoveItem(resource, item);
            changed = true;
        }
    }

    // Add items not in current list (for both purge=true and purge=false)
    var toAdd = desiredItems.Except(currentItems).ToList();
    foreach (var item in toAdd)
    {
        AddItem(resource, item);
        changed = true;
    }
}
```

**Nullability Guidelines:**
- Use `[Nullable(false)]` only on C# nullable properties (those with `?`) where you want to prevent users from submitting `null` values in JSON (e.g., optional strings, booleans)
- Non-nullable C# types (e.g., `string Name` with `[Required]`) don't need `[Nullable(false)]` - they already cannot be null
- DSC canonical properties like `_exist` should use nullable types (e.g., `bool?`) for their C# type, and should have `[Nullable(false)]` to prevent explicit null submission
- The `[Nullable]` attribute controls JSON deserialization behavior, while the `?` on the type controls C# nullability

**Property Read/Write Patterns:**

By default, properties serve **dual purposes** - both as input for `Set()` and output from `Get()`. This is the preferred pattern.

- **Standard properties** (no attribute): Used for both input and output
  - Example: `Value`, `Attributes`, `Members` - accepted as desired state in `Set()`, returned as actual state from `Get()`
  - This is the most common and preferred pattern

- **Write-Only Properties** (`[WriteOnly]`): Only accepted during `Set()`, never returned by `Get()`
  - Use for sensitive data (e.g., passwords) or control properties (e.g., `_purge`)
  - Write-only properties are marked with `"writeOnly": true` in the JSON schema
  - Example: `Password`, `_purge`

- **Read-Only Properties** (`[ReadOnly]`): Only returned by `Get()`, rejected if provided to `Set()`
  - Use for computed or status properties that users cannot directly set
  - Read-only properties are marked with `"readOnly": true` in the JSON schema
  - Example: `State`, `DisplayName`, `Description`, `_metadata`

**Important:** Don't create separate "current" properties for read operations (e.g., `CurrentValue` and `Value`). Instead, use the same property for both input and output unless there's a specific reason to restrict access with `[ReadOnly]` or `[WriteOnly]`.

**Resource Metadata (`_metadata`):**

Resources can return metadata in their results by including a `_metadata` property in the schema returned by `Get()`. The most important metadata property is `_restartRequired`, which indicates what needs to be restarted after a set operation.

When `Set()` needs to communicate metadata (like restart requirements), it should return a `SetResult<Schema>` containing the actual state with metadata attached. DSC automatically calls `Get()` before and after `Set()`, so you don't need to call `Get()` yourself to populate before/after states.

**Important:** When your resource returns actual state from `Set()`, you must add `SetReturn = SetReturn.State` to the `[DscResource]` attribute. When this attribute is set, `Set()` must ALWAYS return a `SetResult<Schema>` with the actual state (never return `null`).

```csharp
[DscResource("OpenDsc.Windows/MyResource", SetReturn = SetReturn.State)]
public sealed class Resource(JsonSerializerContext context)
    : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>
{
    public override string GetSchema()
    {
        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase
        };

        var builder = new JsonSchemaBuilder().FromType<Schema>(config);
        builder.Schema("https://json-schema.org/draft/2020-12/schema");
        var schema = builder.Build();

        return JsonSerializer.Serialize(schema);
    }

    public Schema Get(Schema instance)
    {
        // ... get current state
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        // ... perform the operation

        // When SetReturn.State is used, always return actual state
        var actualState = Get(instance);

        // Example: Add metadata when restart is needed
        if (restartRequired)
        {
            actualState.Metadata = new Dictionary<string, object>
            {
                ["_restartRequired"] = new[]
                {
                    new { system = Environment.MachineName },      // System restart
                    new { service = "serviceName" },               // Service restart
                    new { process = new { name = "app", id = 1234 } }  // Process restart
                }
            };
        }

        return new SetResult<Schema>(actualState);  // Always return actual state when SetReturn.State is set
    }
}
```

**Restart Types:**
- **System restart**: `{ "system": "computerName" }` - indicates the computer needs to restart
- **Service restart**: `{ "service": "serviceName" }` - indicates a specific service needs to restart
- **Process restart**: `{ "process": { "name": "processName", "id": 1234 } }` - indicates a specific process needs to restart

**Example JSON payload** that a resource would return with restart metadata:

```json
{
  "name": "MyFeature",
  "state": "Present",
  "_metadata": {
    "_restartRequired": [
      { "system": "SERVER01" }
    ]
  }
}
```

DSC automatically aggregates `_restartRequired` entries from all resources into the top-level `Microsoft.DSC` metadata in the configuration result.

## Build & Test Workflow

### Building the Project

```powershell
# Build all resources (from repository root)
.\build.ps1

# Build specific configuration
.\build.ps1 -Configuration Debug

# Build portable self-contained version (includes .NET runtime)
.\build.ps1 -Portable

# Build MSI installer
.\build.ps1 -Msi

# Create NuGet packages
.\build.ps1 -Pack

# Skip tests during build
.\build.ps1 -SkipTest
```

**Build Process:**
1. `dotnet publish` compiles the unified executable with platform-specific resources based on target OS
2. Build artifacts are placed in `artifacts/publish/`
3. LCM service is built to `artifacts/Lcm/`
4. Portable builds create self-contained executables with embedded runtime in `artifacts/portable/`
5. MSI builds create installers in `artifacts/msi/`

**Output Structure:**
```
artifacts/
├── publish/
│   ├── OpenDsc.Resources.exe (Windows) / OpenDsc.Resources (Linux/macOS)
│   ├── OpenDsc.Resources.dsc.manifests.json
│   └── ...dependencies
├── Lcm/                         # LCM service
│   ├── OpenDsc.Lcm.exe
│   └── appsettings.json
├── portable/                    # Self-contained with .NET runtime
│   └── OpenDsc.Resources.exe (Windows) / OpenDsc.Resources (Linux/macOS)
└── msi/
    ├── OpenDsc.Resources.msi
    └── OpenDsc.Lcm.msi          # LCM installer
```

### Testing with Pester

Tests are **integration tests only** - they verify end-to-end behavior through the DSC CLI:

```powershell
# Discovery
dsc resource list OpenDsc.Windows/Environment

# Operations
dsc resource get -r OpenDsc.Windows/Environment --input '{"name":"PATH"}'
dsc resource set -r OpenDsc.Windows/Environment --input '{"name":"TEST","value":"123"}'
dsc resource delete -r OpenDsc.Windows/Environment --input '{"name":"TEST"}'
```

**Test Pattern:** Create → Verify → Cleanup (always cleanup in `AfterEach`/`AfterAll`)
**No unit tests** - resources are tested through published executables via DSC CLI

### Admin-Required Test Pattern

Many resources require administrative privileges for certain operations (e.g., creating users, managing services, installing features). Use the following pattern to handle admin-required tests:

```powershell
# Check if running as admin at the start of the test file (Windows only)
if ($IsWindows) {
    $script:isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

Describe 'resource-name' {
    # Non-elevated tests run always
    Context 'Discovery' {
        It 'should be found by dsc' {
            # ... test code
        }
    }

    Context 'Get Operation - Non-Elevated' {
        It 'should return _exist=false for non-existent resource' {
            # ... test code
        }
    }

    # Admin-required tests are skipped when not running as admin
    Context 'Set Operation' -Skip:(!$script:isAdmin) {
        It 'should create resource' {
            # ... test code requiring admin
        }

        AfterEach {
            # Always cleanup test resources
        }
    }

    Context 'Delete Operation' -Skip:(!$script:isAdmin) {
        BeforeEach {
            # Create test resource
        }

        It 'should delete resource' {
            # ... test code
        }
    }
}
```

**Key Points:**
- Use `$script:isAdmin` variable to check elevation status at the start of the test file, wrapped in `if ($IsWindows)` check
- Apply `-Skip:(!$script:isAdmin)` to `Context` blocks that require admin privileges
- Keep non-elevated tests (Discovery, Schema Validation, Get operations) in separate contexts that always run
- Always cleanup test resources in `AfterEach`/`AfterAll` blocks, even in elevated contexts
- Test both elevated and non-elevated scenarios where applicable

## Project-Specific Conventions

### Code Style and Comments

**Comment Usage:**
- **Avoid self-explanatory comments** - code should be self-documenting through clear naming
- **Remove comments that merely restate the code** (e.g., "// Try to find as user first" before `UserPrincipal.FindByIdentity()`)
- **Keep comments only for non-obvious logic** or complex algorithms
- **Empty catch blocks** don't need comments like "// Ignore errors" - the empty block itself indicates the intent
- **File headers** are required (MIT license) - this is enforced by IDE0073

**Examples of comments to avoid:**
```csharp
// BAD: Restates the obvious
// When _purge is true, remove members not in desired list
if (instance.Purge == true) { ... }

// GOOD: No comment needed, code is clear
if (instance.Purge == true) { ... }
```

### Source Generation (Required for AOT)

Resources within the same project share a `SourceGenerationContext.cs` at the project root:

```csharp
// src/OpenDsc.Resource.Windows/SourceGenerationContext.cs
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Group.Schema), TypeInfoPropertyName = "GroupSchema")]
[JsonSerializable(typeof(User.Schema), TypeInfoPropertyName = "UserSchema")]
[JsonSerializable(typeof(Environment.Schema), TypeInfoPropertyName = "EnvironmentSchema")]
// ... all schemas in the project
public partial class SourceGenerationContext : JsonSerializerContext { }
```

Each resource constructor receives the shared context: `new Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default)`

### Error Handling with Exit Codes

Use `[ExitCode]` attributes on `Resource` class:

```csharp
[ExitCode(0, Description = "Success")]
[ExitCode(1, Description = "Invalid parameter")]
[ExitCode(3, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(4, Exception = typeof(SecurityException), Description = "Access denied")]
```

Throw mapped exceptions; framework handles exit codes.

### File Headers (Enforced)

All `.cs` files require MIT license header (IDE0073 warning):

```csharp
// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.
```

### COM Interop Pattern (Shortcut)

For COM interfaces, use P/Invoke declarations with manual RCW management:

```csharp
[ComImport, Guid("..."), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IShellLinkW { /* methods */ }

// Usage: Always release COM objects in finally blocks
try {
    link = (IShellLinkW)new ShellLink();
    // ... use link
} finally {
    if (link != null) Marshal.ReleaseComObject(link);
}
```

## Common Implementation Patterns

### Handling Non-Existent Resources in Get()

```csharp
public Schema Get(Schema instance)
{
    try {
        // Attempt to read resource
        return new Schema { Name = instance.Name, Value = value };
    }
    catch (ResourceNotFoundException) {
        return new Schema { Name = instance.Name, Exist = false };
    }
}
```

### Creating Resources in Set()

```csharp
public SetResult<Schema>? Set(Schema instance)
{
    if (Get(instance).Exist == false)
    {
        CreateResource(instance);  // Helper method
    }

    // Apply configuration changes
    UpdateResource(instance);

    return null;  // Or return SetResult with before/after states
}
```

### Scope/Privilege Patterns

Check scope in Get/Set/Delete for user vs. machine operations (see [Environment](../src/OpenDsc.Resource.Windows/Environment/)):

```csharp
var target = instance.Scope is Scope.Machine
    ? EnvironmentVariableTarget.Machine
    : EnvironmentVariableTarget.User;
```

Machine scope requires admin elevation.

## Local Configuration Manager (LCM) Service

The LCM is a background service (`src/OpenDsc.Lcm/`) that continuously monitors and optionally remediates DSC configurations. It operates in two modes and is implemented as a cross-platform .NET service.

### Architecture Overview

**Key Components:**
- `LcmWorker.cs` - Main background service implementing two operational modes
- `DscExecutor.cs` - Executes DSC CLI commands (`dsc config test` and `dsc config set`)
- `LcmConfig.cs` - Configuration model with validation
- `ConfigPaths.cs` - Platform-specific configuration paths

**Operational Modes:**
1. **Monitor Mode** - Periodically runs `dsc config test` to detect drift
2. **Remediate Mode** - Runs `dsc config test` and applies `dsc config set` when drift detected

### Configuration Structure

```csharp
public class LcmConfig
{
    public ConfigurationMode ConfigurationMode { get; set; } = ConfigurationMode.Monitor;
    public string ConfigurationPath { get; set; }  // Path to main.dsc.yaml
    public TimeSpan ConfigurationModeInterval { get; set; } = TimeSpan.FromMinutes(15);
    public string? DscExecutablePath { get; set; }  // Optional, defaults to 'dsc' in PATH
}
```

**Configuration Sources (priority order):**
1. Command-line arguments
2. Environment variables (prefixed with `LCM_`)
3. System-wide config (platform-specific):
   - Linux: `/etc/opendsc/lcm/appsettings.json`
   - macOS: `/Library/Preferences/OpenDSC/LCM/appsettings.json`
   - Windows: `%ProgramData%\OpenDSC\LCM\appsettings.json`
4. Bundled `appsettings.json` in service directory
5. Environment-specific: `appsettings.{Environment}.json`

### Building and Deploying LCM

```powershell
# Build LCM service
.\build.ps1  # Outputs to artifacts/Lcm/

# Build MSI installer (Windows only)
.\build.ps1 -Msi  # Outputs to artifacts/msi/

# Service is built for:
# - Windows: net10.0-windows (can be installed as Windows Service)
# - Linux: net10.0 (can run as systemd service)
```

**Platform-Specific Deployment:**
- Windows: MSI installer registers as Windows Service
- Linux/macOS: Run as console app or systemd/launchd service

### Key Patterns

**Dynamic Mode Switching:**
The LCM watches configuration changes and gracefully switches between Monitor and Remediate modes without restarting:

```csharp
_configChangeToken = lcmMonitor.OnChange((config, name) => {
    OnConfigurationReloaded(config);  // Cancels current operation, switches mode
});
```

**DSC Execution Pattern:**
The `DscExecutor` invokes DSC CLI with structured output parsing:

```csharp
var result = await dscExecutor.ExecuteTestAsync(config.ConfigurationPath, config, LogLevel.Information);
// Returns parsed DscResult with messages, results, and hadErrors flag
```

**Logging:**
Uses source-generated logging with `LoggerMessage` attributes for performance. All log messages are defined as partial methods in the worker classes.

### Working with LCM

**Testing Locally:**
```powershell
# Set configuration via environment
$env:LCM_ConfigurationPath = "C:\configs\main.dsc.yaml"
$env:LCM_ConfigurationMode = "Monitor"
$env:LCM_ConfigurationModeInterval = "00:05:00"

# Run as console app
.\artifacts\Lcm\OpenDsc.Lcm.exe
```

**Configuration File Example:**
```json
{
  "LCM": {
    "ConfigurationMode": "Remediate",
    "ConfigurationPath": "/etc/opendsc/config/main.dsc.yaml",
    "ConfigurationModeInterval": "00:15:00",
    "DscExecutablePath": "/usr/local/bin/dsc"
  }
}
```

## Creating a New Resource

### Adding a Resource to an Existing Project

Use [Environment/](../src/OpenDsc.Resource.Windows/Environment/) as the template - it's the simplest, most complete example.

1. **Create resource folder** in the appropriate project:
   - Windows-only: `src/OpenDsc.Resource.Windows/<Name>/`
   - Cross-platform: `src/OpenDsc.Resource.FileSystem/<Name>/`, `src/OpenDsc.Resource.Xml/<Name>/`, or `src/OpenDsc.Resource.Archive/<Area>/<Name>/`

2. **Create core files** in the resource folder:
   - `Resource.cs` - Core implementation with `[DscResource]` attribute
   - `Schema.cs` - Property definitions with JSON Schema attributes
   - Supporting types (e.g., `Scope.cs`, enums) if needed

3. **Implement Resource class:**
   - Inherit from `DscResource<Schema>` with context parameter
   - Add `[DscResource("OpenDsc.Windows/<Name>", "0.1.0")]` attribute with version and metadata
   - Override `GetSchema()` method with standard implementation
   - Define `[ExitCode]` mappings for exceptions
   - Implement capability interfaces (all optional, but implement those that make sense):
     - `IGettable<Schema>` - return current state or `Exist = false`
     - `ISettable<Schema>` - apply changes
     - `IDeletable<Schema>` - remove resource
     - `IExportable<Schema>` - enumerate all instances

4. **Define Schema:**
   - Add `[Title]`, `[Description]`, `[AdditionalProperties(false)]`
   - Required key property with `[Required]` and `[Pattern]` validation
   - Use `[Nullable(false)]` on nullable C# properties where you want to prevent `null` JSON values
   - Use `[JsonPropertyName("_propertyName")]` for DSC canonical properties
   - Add `[Default]` values where appropriate
   - All enums use PascalCase values

5. **Update SourceGenerationContext.cs** in the project root:
   ```csharp
   [JsonSerializable(typeof(YourResource.Schema), TypeInfoPropertyName = "YourResourceSchema")]
   ```

6. **Register resource in Program.cs** of the platform executable:
   ```csharp
   // Add namespace alias
   using YourResourceNs = OpenDsc.Resource.Windows.YourResource;

   // Instantiate resource
   var yourResource = new YourResourceNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);

   // Add to CommandBuilder
   .AddResource<YourResourceNs.Resource, YourResourceNs.Schema>(yourResource)
   ```

7. **Write integration tests** (`tests/YourResource.Tests.ps1`):
   - Discovery: `dsc resource list OpenDsc.Windows/<Name>`
   - Get operation: test existing and non-existent resources
   - Set operation: create and update
   - Delete operation: remove and verify
   - Export operation (if implemented)
   - Use admin-required test pattern with `-Skip:(!$script:isAdmin)` for elevated operations
   - Always cleanup test resources in `AfterEach`/`AfterAll`

8. **Fix IDE problems:**
   - Check and resolve all IDE/VS Code diagnostics (warnings, errors, suggestions)
   - Remove unused variables, unnecessary using directives, and other code issues
   - Ensure MIT license header is present in all `.cs` files

9. **Verify:**
   - Build succeeds: `.\build.ps1`
   - Tests pass: all Pester tests green
   - Manual test: `dsc resource get -r OpenDsc.Windows/<Name> --input '{...}'`

## Key Files to Reference

- **Template Resource:** [Environment/](../src/OpenDsc.Resource.Windows/Environment/) (simplest, most complete implementation)
- **Complex Resource:** [OptionalFeature/](../src/OpenDsc.Resource.Windows/OptionalFeature/) (uses SetReturn.State, metadata, and restart handling)
- **Collection Management:** [Group/](../src/OpenDsc.Resource.Windows/Group/) (demonstrates _purge pattern)
- **COM Interop:** [Shortcut/](../src/OpenDsc.Resource.Windows/Shortcut/) (P/Invoke patterns for COM)
- **Win32 API:** [Service/](../src/OpenDsc.Resource.Windows/Service/) (Win32 API wrappers)
- **DISM API:** [OptionalFeature/](../src/OpenDsc.Resource.Windows/OptionalFeature/) (P/Invoke DISM interop)
- **Archive Resources:** [Zip/Compress/](../src/OpenDsc.Resource.Archive/Zip/Compress/), [Zip/Expand/](../src/OpenDsc.Resource.Archive/Zip/Expand/) (multi-level namespace structure)
- **LCM Service:** [LcmWorker.cs](../src/OpenDsc.Lcm/LcmWorker.cs), [DscExecutor.cs](../src/OpenDsc.Lcm/DscExecutor.cs) (background service patterns)
- **Test Examples:** [Environment.Tests.ps1](../tests/Windows/Environment.Tests.ps1) (comprehensive integration tests)
- **Platform Entry Point:** [Program.cs](../src/OpenDsc.Resources/Program.cs) (resource registration with conditional compilation)
- **Scheduled Task Resource:** [ScheduledTask/](../src/OpenDsc.Resource.Windows/ScheduledTask/) (complex resource with embedded schema, multiple supporting types)
- **POSIX Resources:** [FileSystem/Permission/](../src/OpenDsc.Resource.Posix/FileSystem/Permission/) (cross-platform file permissions)
- **Editor Config:** [.editorconfig](../.editorconfig) (C# style rules, file headers)

## Package Dependencies

All resources depend on:
- `OpenDsc.Resource.CommandLine` - Version 0.4.0 (provides DscResource base class, interfaces, and CLI framework)
- `JsonSchema.Net.Generation` - Version 5.1.1 (JSON Schema generation attributes)

Platform-specific dependencies:
- `System.DirectoryServices.AccountManagement` - Version 9.0.0 for user/group management
- `System.ServiceProcess.ServiceController` - Version 9.0.0 for service management
- Windows COM/Win32 APIs - P/Invoke declarations for shortcut, service, and DISM operations

**Target Frameworks:**
- Libraries: .NET Standard 2.0, .NET 8, .NET 9, .NET 10 (multi-targeting)
- Executables: .NET 10 (Windows executables use `net10.0-windows`)
