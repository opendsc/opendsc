# OpenDsc.Parameters

A C# library for hierarchical parameter merging with scope-based precedence for
DSC configurations. Supports YAML and JSON parameter files with provenance
tracking to trace parameter value origins across multiple scopes.

## Features

- 🔀 **Hierarchical Merging** - Merge parameter files across multiple scopes
  with precedence-based overrides
- 📊 **Provenance Tracking** - Track where each parameter value originated and
  what values were overridden
- 📝 **Format Support** - Read and write both YAML and JSON formats
- 🎯 **Deep Merging** - Recursively merge nested objects while replacing arrays
- 🔧 **Flexible Output** - Choose output format independently from input
- 📦 **Multi-Target** - Supports .NET Standard 2.0, .NET 8, .NET 9, and .NET 10

## Installation

```powershell
dotnet add package OpenDsc.Parameters
```

## Usage

### Basic Merging

Merge multiple parameter files with precedence-based overrides (first file has
lowest precedence, last file has highest):

```csharp
using OpenDsc.Parameters;

var merger = new ParameterMerger();

// Merge YAML files
var globalYaml = @"
server: localhost
port: 8080
database: dev
";

var prodYaml = @"
server: production.example.com
database: production
";

var merged = merger.Merge([globalYaml, prodYaml]);

// Result (YAML):
// server: production.example.com
// port: 8080
// database: production
```

### Format Conversion

Convert between YAML and JSON formats:

```csharp
var merger = new ParameterMerger();

var yamlInput = "server: localhost\nport: 8080";

// Convert to JSON
var jsonOutput = merger.Merge(
    [yamlInput],
    new MergeOptions { OutputFormat = ParameterFormat.Json }
);

// Result: {"server":"localhost","port":8080}
```

### Provenance Tracking

Track where each parameter value originated and view override history:

```csharp
var merger = new ParameterMerger();

var sources = new[]
{
    new ParameterSource
    {
        ScopeName = "Global",
        Precedence = 1,
        Content = "server: localhost\nport: 8080"
    },
    new ParameterSource
    {
        ScopeName = "Production",
        Precedence = 2,
        Content = "server: production.example.com"
    }
};

var result = merger.MergeWithProvenance(sources);

// Access merged content
Console.WriteLine(result.MergedContent);

// Check provenance for 'server' parameter
var serverProvenance = result.Provenance["server.server"];
Console.WriteLine($"Value: {serverProvenance.Value}");
Console.WriteLine($"From: {serverProvenance.ScopeName}");
Console.WriteLine($"Precedence: {serverProvenance.Precedence}");

// Check if value was overridden
if (serverProvenance.OverriddenValues != null)
{
    foreach (var override in serverProvenance.OverriddenValues)
    {
        Console.WriteLine($"Previously: {override.Value} (from {override.ScopeName})");
    }
}
```

### Deep Object Merging

Nested objects are merged recursively:

```csharp
var base = @"
config:
  keep: original
  replace: old
";

var override = @"
config:
  replace: new
  add: additional
";

var merged = merger.Merge([base, override]);

// Result:
// config:
//   keep: original
//   replace: new
//   add: additional
```

### Array Replacement

Arrays are replaced entirely, not merged:

```csharp
var base = @"
servers:
  - server1
  - server2
";

var override = @"
servers:
  - server3
";

var merged = merger.Merge([base, override]);

// Result:
// servers:
//   - server3
```

## API Reference

### ParameterMerger

Main class for merging parameter files.

#### Methods

##### `Merge(IEnumerable<string> parameterFiles, MergeOptions? options = null)`

Merges multiple parameter files in precedence order (first = lowest, last =
highest).

**Parameters:**

- `parameterFiles` - Collection of YAML or JSON parameter file contents
- `options` - Optional merge options

**Returns:** Merged parameter content as string

##### `MergeWithProvenance(IEnumerable<ParameterSource> parameterFiles, MergeOptions? options = null)`

Merges parameter files with provenance tracking.

**Parameters:**

- `parameterFiles` - Collection of parameter sources with scope information
- `options` - Optional merge options

**Returns:** `MergeResult` containing merged content and provenance information

### MergeOptions

Options for controlling merge behavior.

**Properties:**

- `OutputFormat` - Output format (`ParameterFormat.Yaml` or
  `ParameterFormat.Json`). Default: `Yaml`
- `IncludeComments` - Whether to include comments in YAML output. Default:
  `false`

### ParameterSource

Represents a parameter file with scope information.

**Properties:**

- `ScopeName` (required) - Name of the scope (e.g., "Global", "Production")
- `Precedence` (required) - Precedence value (higher = higher priority)
- `Content` (required) - YAML or JSON parameter file content

### MergeResult

Result of a merge operation with provenance.

**Properties:**

- `MergedContent` - The merged parameter content in the specified output format
- `Provenance` - Dictionary mapping parameter paths to their provenance
  information

### ParameterProvenance

Tracks the origin of a parameter value.

**Properties:**

- `ScopeName` - Scope where this value originated
- `Precedence` - Precedence value of the scope
- `Value` - The parameter value
- `OverriddenValues` - List of previous values that were overridden (null if
  no overrides)

### ScopeValue

Represents a parameter value from a specific scope.

**Properties:**

- `ScopeName` - Scope name
- `Precedence` - Precedence value
- `Value` - Parameter value

## Behavior Notes

### Provenance Tracking Behavior

- **First Source Baseline**: Keys from the first source are not tracked in
  provenance as they serve as the baseline
- **Leaf Values Only**: Provenance tracks only when leaf values are added or
  replaced, not intermediate object paths
- **Override Tracking**: Only the most recent override is tracked for each
  parameter path

### Merge Rules

1. **Objects**: Recursively merged (keys from all sources are combined)
2. **Arrays**: Completely replaced (last source wins)
3. **Primitives**: Replaced (last source wins)
4. **Null Values**: Treated as regular values and can override non-null values

## Use Cases

### DSC Configuration Parameters

Use with the OpenDSC Pull Server to manage parameters across multiple scopes:

```csharp
// Global scope (lowest precedence)
var globalParams = "logLevel: Info\ntimeout: 30";

// Environment scope (higher precedence)
var envParams = "logLevel: Debug\nserver: prod.example.com";

// Node-specific scope (highest precedence)
var nodeParams = "server: node1.example.com";

var merged = merger.Merge([globalParams, envParams, nodeParams]);
```

### Environment-Specific Configuration

Manage configuration across development, staging, and production:

```csharp
var sources = new[]
{
    new ParameterSource
    {
        ScopeName = "Base",
        Precedence = 1,
        Content = File.ReadAllText("config/base.yaml")
    },
    new ParameterSource
    {
        ScopeName = Environment.GetEnvironmentVariable("ENVIRONMENT") ?? "Development",
        Precedence = 2,
        Content = File.ReadAllText($"config/{env}.yaml")
    }
};

var result = merger.MergeWithProvenance(sources);
```

### Multi-Tenant Configuration

Layer configuration with customer-specific overrides:

```csharp
var sources = new[]
{
    new ParameterSource { ScopeName = "Default", Precedence = 1, Content = defaultConfig },
    new ParameterSource { ScopeName = "Customer-A", Precedence = 2, Content = customerConfig }
};
```

## Integration with OpenDSC

This library is used by the OpenDSC Pull Server (`OpenDsc.Server`) to merge
parameters across scopes for managed DSC configurations. The
`ParameterMergeService` uses this library to:

1. Query scope assignments for a node
2. Load parameter files for each assigned scope
3. Merge parameters in precedence order
4. Return the merged configuration to the LCM

## Requirements

- .NET Standard 2.0 or later
- .NET 8+ for latest features

## Dependencies

- **YamlDotNet** - YAML parsing and serialization
- **System.Text.Json** - JSON support (.NET Standard 2.0 only, built-in for
  .NET 8+)
- **PolySharp** - C# language feature polyfills (.NET Standard 2.0 only)

## License

MIT
