---
name: create-dsc-resource
description: "WORKFLOW SKILL — Create a new DSC resource end-to-end. USE FOR: adding a new OpenDsc.Windows, OpenDsc.SqlServer, or cross-platform resource (FileSystem, Xml, Json, Archive, Posix). Guides through: Schema.cs, Resource.cs, SourceGenerationContext update, Program.cs registration, and xUnit test stubs. INVOKES: file search, read, create, and edit tools. DO NOT USE FOR: modifying existing resources (just edit directly); adding Pull Server endpoints (use /add-pull-server-endpoint)."
---

# Create DSC Resource

## Overview

This skill creates a complete, working DSC resource: `Schema.cs`, `Resource.cs`, any supporting types, `SourceGenerationContext.cs` update, `Program.cs` registration, and xUnit test stubs.

Use the template files in this skill directory as starting points:
- [`Resource.cs.template`](./Resource.cs.template) — resource class boilerplate
- [`Schema.cs.template`](./Schema.cs.template) — schema class boilerplate

The canonical reference implementation is [`src/OpenDsc.Resource.Windows/Environment/`](../../../src/OpenDsc.Resource.Windows/Environment/) — read it before starting.

## Required Inputs

Before working, determine:
1. **Resource name** (e.g., `Registry`, `FirewallRule`)
2. **Area**: `Windows`, `SqlServer`, or cross-platform (`FileSystem`, `Xml`, `Json`, `Archive`, `Posix`)
3. **DSC resource ID**: `OpenDsc.{Area}/{Name}` (e.g., `OpenDsc.Windows/Registry`)
4. **Design pattern** — see quick reference below
5. **Description** and **tags** for `[DscResource]`
6. **Key property** (the identifying property, e.g., `Name`, `Key`, `Path`)
7. **Additional configurable properties**

## Pattern Quick Reference

| Pattern | Has `_exist` | Has `IDeletable` | Has `_purge` | Use For |
|---------|-------------|-----------------|-------------|---------|
| 1 — Instance | ✅ | ✅ | ❌ | User, File, Service, Registry key |
| 2 — Pure List | ❌ | ❌ | ✅ | ACL rules, User rights |
| 3 — Hybrid | ✅ | ✅ | ✅ | Group+Members, Element+Attributes |

## Step-by-Step Process

### Step 1 — Read template files

Read [`Resource.cs.template`](./Resource.cs.template) and [`Schema.cs.template`](./Schema.cs.template) from this skill directory, then read the canonical reference:
- [`src/OpenDsc.Resource.Windows/Environment/Resource.cs`](../../../src/OpenDsc.Resource.Windows/Environment/Resource.cs)
- [`src/OpenDsc.Resource.Windows/Environment/Schema.cs`](../../../src/OpenDsc.Resource.Windows/Environment/Schema.cs)

### Step 2 — Create resource folder and files

Create `src/OpenDsc.Resource.{Area}/{Name}/`:
- `Resource.cs` — based on `Resource.cs.template`; fill in namespace, DSC resource ID, description, tags, and implement all applicable interfaces
- `Schema.cs` — based on `Schema.cs.template`; add all properties with correct attributes
- Supporting types (e.g., `Scope.cs`, enums) if needed

**Resource.cs checklist:**
- [ ] Correct namespace: `OpenDsc.Resource.{Area}.{Name}`
- [ ] `[DscResource("OpenDsc.{Area}/{Name}", "0.1.0", Description = "...", Tags = [...])]`
- [ ] `[ExitCode]` attributes for expected exception types
- [ ] `GetSchema()` using standard `JsonSchemaBuilder` pattern
- [ ] Applicable interfaces implemented (`IGettable`, `ISettable`, `IDeletable`, `IExportable`)
- [ ] MIT license header

**Schema.cs checklist:**
- [ ] `[Title]`, `[Description]`, `[AdditionalProperties(false)]`
- [ ] `[Required]` on the key property
- [ ] `_exist` (Pattern 1/3) or `_purge` (Pattern 2/3) as appropriate
- [ ] `[Nullable(false)]` on nullable C# properties where null should not be allowed
- [ ] `[WriteOnly]` on passwords and `_purge`
- [ ] MIT license header

### Step 3 — Update SourceGenerationContext.cs

Read `src/OpenDsc.Resource.{Area}/SourceGenerationContext.cs`, then add:

```csharp
[JsonSerializable(typeof({Name}.Schema), TypeInfoPropertyName = "{Name}Schema")]
```

### Step 4 — Register in Program.cs

Read [`src/OpenDsc.Resources/Program.cs`](../../../src/OpenDsc.Resources/Program.cs) to find the correct insertion points, then add:

```csharp
// At top — namespace alias (inside #if WINDOWS block for Windows-only)
using {Name}Ns = OpenDsc.Resource.{Area}.{Name};

// Instantiation
var {name}Resource = new {Name}Ns.Resource(OpenDsc.Resource.{Area}.SourceGenerationContext.Default);

// Registration on CommandBuilder
command.AddResource<{Name}Ns.Resource, {Name}Ns.Schema>({name}Resource);
```

Windows-only resources go inside `#if WINDOWS`. Cross-platform resources are unconditional. Linux/macOS-only (Posix) go inside `#if !WINDOWS` with a runtime OS check.

### Step 5 — Write xUnit tests

Create `tests/OpenDsc.Resource.{Area}.Tests/{Name}/{Name}Tests.cs`:

- Schema generation: `GetSchema()` returns valid JSON with expected properties
- Serialization roundtrip: JSON → Schema → JSON preserves all fields
- `Get()`: returns correct state for existing resource; returns `Exist = false` for missing resource
- `Set()`: creates resource when not present; updates when present
- `Delete()`: removes resource (if `IDeletable`)
- `Export()`: enumerates instances (if `IExportable`)

### Step 6 — Fix IDE diagnostics

- Resolve all warnings/errors in new files
- Remove unused `using` directives
- Confirm MIT license header is present in all `.cs` files

### Step 7 — Verify build

```powershell
.\build.ps1 -SkipTest
```

### Step 8 — Run new tests

```powershell
dotnet test tests/OpenDsc.Resource.{Area}.Tests/ --filter Category=Integration
```

## Notes

- Check [`src/OpenDsc.Resource.Windows/Group/`](../../../src/OpenDsc.Resource.Windows/Group/) for a complete Hybrid (Pattern 3) example
- Check [`src/OpenDsc.Resource.Windows/UserRight/`](../../../src/OpenDsc.Resource.Windows/UserRight/) for a complete Pure List (Pattern 2) example
- Check [`src/OpenDsc.Resource.Windows/OptionalFeature/`](../../../src/OpenDsc.Resource.Windows/OptionalFeature/) for `SetReturn.State` and restart metadata
- SQL Server resources use SMO — read [`src/OpenDsc.Resource.SqlServer/Login/`](../../../src/OpenDsc.Resource.SqlServer/Login/) as a reference
