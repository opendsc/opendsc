---
description: "Use when creating or modifying DSC resources, resource schemas, or the resource executable entry point (Program.cs). Covers resource class inheritance, schema conventions, _exist/_purge design patterns, GetSchema(), SourceGenerationContext, and common Get/Set/Delete implementation patterns."
applyTo: "src/OpenDsc.Resource*/**,src/OpenDsc.Resources/**"
---

# DSC Resource Implementation

## Resource Structure

Each resource is a folder within a shared project library:

```
src/OpenDsc.Resource.Windows/
├── OpenDsc.Resource.Windows.csproj
├── SourceGenerationContext.cs         # Shared JSON source generation for all schemas in the project
├── Environment/
│   ├── Resource.cs                    # Core logic with [DscResource] attribute
│   ├── Schema.cs                      # JSON schema model
│   └── Scope.cs                       # Supporting types (enums, etc.)
└── ...
```

Template resource: [`src/OpenDsc.Resource.Windows/Environment/`](../../src/OpenDsc.Resource.Windows/Environment/) — simplest, most complete example.

## Resource Class Pattern

All resources inherit from `DscResource<Schema>` and implement capability interfaces:

```csharp
[DscResource("OpenDsc.Windows/Environment", "0.1.0", Description = "...", Tags = ["windows", "environment"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
public sealed class Resource(JsonSerializerContext context)
    : DscResource<Schema>(context),
      IGettable<Schema>,
      ISettable<Schema>,
      IDeletable<Schema>,
      IExportable<Schema>
{
    public override string GetSchema() { ... }
    public Schema Get(Schema instance) { ... }
    public SetResult<Schema>? Set(Schema instance) { ... }
    public void Delete(Schema instance) { ... }
    public IEnumerable<Schema> Export(Schema? filter) { ... }
}
```

All interfaces are optional — implement those that make sense. Strive to implement all applicable ones.

**Interface contracts:**
- `IGettable.Get(Schema)` — return current state; set `Exist = false` if not found (never explicitly set `Exist = true`)
- `ISettable.Set(Schema)` — apply changes; do NOT check `_exist` (DSC engine routes: `_exist=true` → `Set()`, `_exist=false` → `Delete()`)
- `IDeletable.Delete(Schema)` — remove resource
- `IExportable.Export(Schema? filter)` — yield all instances, optionally filtered

## GetSchema() Method

**Source Generation Pattern** (standard for all resources):

The compiler automatically generates JSON schemas from the `Schema` class decorated with `[GenerateJsonSchema]`. `GetSchema()` retrieves and bundles this generated schema:

```csharp
public override string GetSchema()
{
    var registry = new SchemaRegistry();
    var schema = registry.CreateBundle(GeneratedJsonSchemas.{Name}_Schema.BaseUri, Schema.BundleUri);
    return JsonSerializer.Serialize(schema, SourceGenerationContext.Default.JsonSchema);
}
```

The compiler generates `GeneratedJsonSchemas.{Name}_Schema` automatically when the `Schema` class has `[GenerateJsonSchema]` attribute.

**Alternative: Embedded schema** for complex resources (e.g., ScheduledTask with nested objects that the generator cannot handle):

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

Add to `.csproj`: `<EmbeddedResource Include="ScheduledTask\schema.json" />`

## Schema Class

```csharp
[Id("https://opendsc.dev/schemas/v1/windows/myresource.schema.json")]
[Title("...")]
[Description("...")]
[AdditionalProperties(false)]
[GenerateJsonSchema]
public sealed class Schema
{
    public static readonly Uri BundleUri = new("https://opendsc.dev/schemas/v1/bundled/windows/myresource.schema.json");

    [Required]
    [Pattern(@"regex")]
    public string Name { get; set; }

    [WriteOnly]           // Only accepted in Set(), never returned by Get()
    [Nullable(false)]
    public string? Password { get; set; }

    [JsonPropertyName("_exist")]
    [Default(true)]
    [Nullable(false)]
    public bool? Exist { get; set; }
}
```

**Key attributes:**
- `[Id]` — the canonical schema URI for this resource (format: `https://opendsc.dev/schemas/v1/{area}/{name}.schema.json`)
- `[GenerateJsonSchema]` — signals the compiler to generate a static schema (produces `GeneratedJsonSchemas.{Name}_Schema`)
- `static readonly Uri BundleUri` — the bundled (with imports resolved) schema URI

**Naming:** user-facing properties in camelCase; DSC canonical properties prefixed with `_` (`_exist`, `_purge`, `_inDesiredState`); enum values in PascalCase.

## DSC Canonical Properties

- **`_exist` (bool?, default true)** — lifecycle control; DSC calls `Set()` when true/omitted, `Delete()` when false
- **`_purge` (bool?, default false)** — collection control; `false` = additive, `true` = exact/purge; always `[WriteOnly]`
- **`_inDesiredState` (bool?)** — read-only; for resources implementing `ITestable<Schema>`

## Resource Design Patterns

Choose ONE pattern:

**Pattern 1 — Instance Management** (`_exist` + `IDeletable`)
- Use when: managing existence of a single instance (User, Group, File, Service, Environment variable)
- `_exist=true` → create/update via `Set()`; `_exist=false` → delete via `Delete()`

**Pattern 2 — Pure List Management** (`_purge`, NO `_exist`, NO `IDeletable`)
- Use when: managing a collection where the container must pre-exist (ACL rules, user rights)
- `_purge=false` → additive; `_purge=true` → exact (remove items not in list)
- Examples: [`FileSystem/Acl/`](../../src/OpenDsc.Resource.Windows/FileSystem/Acl/), [`UserRight/`](../../src/OpenDsc.Resource.Windows/UserRight/)

**Pattern 3 — Hybrid** (`_exist` + `_purge` + `IDeletable`)
- Use when: managing a container that also owns a list (Group + Members, Xml/Element + Attributes)
- `_exist` controls the container; `_purge` controls the list items
- Examples: [`Group/`](../../src/OpenDsc.Resource.Windows/Group/)

**Anti-patterns:**
- ❌ Do NOT add `_exist` or `IDeletable` to pure list resources
- ❌ Do NOT add `_purge` to instance resources unless they also manage a container list

## `_purge` Implementation Pattern

```csharp
if (instance.Items != null)
{
    var current = new HashSet<string>(GetCurrentItems(resource), StringComparer.OrdinalIgnoreCase);
    var desired = new HashSet<string>(instance.Items, StringComparer.OrdinalIgnoreCase);

    if (instance.Purge == true)
    {
        foreach (var item in current.Except(desired).ToList())
            RemoveItem(resource, item);
    }

    foreach (var item in desired.Except(current).ToList())
        AddItem(resource, item);
}
```

## Nullability Guidelines

- `[Nullable(false)]` only on C# nullable (`?`) properties — prevents `null` JSON values
- Non-nullable C# types (`string Name`) don't need it — already cannot be null
- DSC canonical properties (`bool? Exist`, `bool? Purge`) should use nullable C# type + `[Nullable(false)]`

## Property Read/Write Patterns

- **Standard (no attribute)**: dual-purpose — input to `Set()` and output from `Get()` — **preferred default**
- **`[WriteOnly]`**: only accepted by `Set()`, never returned by `Get()`. Use for passwords, `_purge`
- **`[ReadOnly]`**: only returned by `Get()`, rejected in `Set()`. Use for computed/status properties

Do NOT create separate "current" properties (e.g., `CurrentValue` + `Value`). Use a single property.

## `_metadata` and `SetReturn.State`

When `Set()` needs to return actual state (e.g., with restart metadata), add `SetReturn = SetReturn.State` to `[DscResource]`. When set, `Set()` MUST always return a non-null `SetResult<Schema>`.

```csharp
[DscResource("OpenDsc.Windows/MyResource", SetReturn = SetReturn.State)]
...
public SetResult<Schema>? Set(Schema instance)
{
    var actualState = Get(instance);

    if (restartRequired)
    {
        actualState.Metadata = new Dictionary<string, object>
        {
            ["_restartRequired"] = new[]
            {
                new { system = Environment.MachineName },
                new { service = "serviceName" },
                new { process = new { name = "app", id = 1234 } }
            }
        };
    }

    return new SetResult<Schema>(actualState);
}
```

Restart types: `{ system: "hostname" }`, `{ service: "name" }`, `{ process: { name: "...", id: 1234 } }`

Reference: [`OptionalFeature/`](../../src/OpenDsc.Resource.Windows/OptionalFeature/) — uses `SetReturn.State`, metadata, and restart handling.

## SourceGenerationContext

All schemas in a project share a `SourceGenerationContext.cs` at the project root:

```csharp
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Group.Schema), TypeInfoPropertyName = "GroupSchema")]
[JsonSerializable(typeof(Environment.Schema), TypeInfoPropertyName = "EnvironmentSchema")]
// ... every schema in the project
public partial class SourceGenerationContext : JsonSerializerContext { }
```

Pass the shared context when instantiating: `new Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default)`

## Program.cs Registration

Register resources in [`src/OpenDsc.Resources/Program.cs`](../../src/OpenDsc.Resources/Program.cs) using conditional compilation:

```csharp
#if WINDOWS
using EnvironmentNs = OpenDsc.Resource.Windows.Environment;
#endif

#if WINDOWS
var environmentResource = new EnvironmentNs.Resource(OpenDsc.Resource.Windows.SourceGenerationContext.Default);
#endif

var command = new CommandBuilder();

#if WINDOWS
command.AddResource<EnvironmentNs.Resource, EnvironmentNs.Schema>(environmentResource);
#endif

return command.Build().Parse(args).Invoke();
```

- Windows-only resources: inside `#if WINDOWS`
- Linux/macOS-only (POSIX): inside `#if !WINDOWS` with runtime OS check
- Cross-platform resources: unconditional

## Common Implementation Patterns

**Get() — non-existent resource:**

```csharp
public Schema Get(Schema instance)
{
    try { return new Schema { Name = instance.Name, Value = value }; }
    catch (ResourceNotFoundException) { return new Schema { Name = instance.Name, Exist = false }; }
}
```

**Set() — create or update:**

```csharp
public SetResult<Schema>? Set(Schema instance)
{
    if (Get(instance).Exist == false) CreateResource(instance);
    UpdateResource(instance);
    return null;
}
```

**Scope pattern (user vs. machine):**

```csharp
var target = instance.Scope is DscScope.Machine
    ? EnvironmentVariableTarget.Machine
    : EnvironmentVariableTarget.User;
```

**COM interop — always release in finally:**

```csharp
try { link = (IShellLinkW)new ShellLink(); /* use link */ }
finally { if (link != null) Marshal.ReleaseComObject(link); }
```

## Exit Codes

```csharp
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(3, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(4, Exception = typeof(SecurityException), Description = "Access denied")]
```

Throw the mapped exception type; the framework converts it to the correct exit code.

## Key Reference Resources

| Resource | Path | Pattern |
|----------|------|---------|
| Template (simplest) | [`Environment/`](../../src/OpenDsc.Resource.Windows/Environment/) | Instance management |
| SetReturn.State + restart | [`OptionalFeature/`](../../src/OpenDsc.Resource.Windows/OptionalFeature/) | Complex with metadata |
| Hybrid `_exist` + `_purge` | [`Group/`](../../src/OpenDsc.Resource.Windows/Group/) | Container + list |
| Pure list `_purge` | [`UserRight/`](../../src/OpenDsc.Resource.Windows/UserRight/) | List only |
| COM interop | [`Shortcut/`](../../src/OpenDsc.Resource.Windows/Shortcut/) | P/Invoke COM |
| Win32 API | [`Service/`](../../src/OpenDsc.Resource.Windows/Service/) | Win32 API wrappers |
| SQL Server (SMO) | [`Login/`](../../src/OpenDsc.Resource.SqlServer/Login/) | SQL management |
| Embedded schema | [`ScheduledTask/`](../../src/OpenDsc.Resource.Windows/ScheduledTask/) | Complex schema |
| POSIX | [`FileSystem/Permission/`](../../src/OpenDsc.Resource.Posix/FileSystem/Permission/) | Cross-platform |
