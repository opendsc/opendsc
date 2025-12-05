# OpenDsc.Resource.CommandLine

The `OpenDsc.Resource.CommandLine` package contains a command-line and
resource manifest generator for Microsoft DSC v3 resources.

## Installation

Install the package via NuGet:

```sh
dotnet add package OpenDsc.Resource.CommandLine
```

## Usage

This package provides a `CommandBuilder<TResource, TSchema>` class that
generates a `System.CommandLine.RootCommand` for your DSC resource. The
command structure includes:

- `config`: Manage resource configuration
  - `get`: Retrieve resource configuration
  - `set`: Set resource configuration
  - `test`: Test resource configuration
  - `delete`: Delete resource configuration
  - `export`: Export resource configuration
- `schema`: Retrieve resource JSON schema
- `manifest`: Retrieve resource manifest

### Example

```csharp
using OpenDsc.Resource.CommandLine;
using System.CommandLine;
using System.Text.Json;

var resource = new Resource(SourceGenerationContext.Default);
var command = CommandBuilder<Resource, Schema>.Build(resource, SourceGenerationContext.Default);
return command.Parse(args).Invoke();
```

## Commands

### config get

Retrieves the current configuration of the resource.

```sh
app config get --input '{"property": "value"}'
```

### config set

Sets the configuration of the resource.

```sh
app config set --input '{"property": "value"}'
```

### config test

Tests the configuration of the resource.

```sh
app config test --input '{"property": "value"}'
```

### config delete

Deletes the configuration of the resource.

```sh
app config delete --input '{"property": "value"}'
```

### config export

Exports the configuration of the resource.

```sh
app config export
```

### schema

Outputs the JSON schema for the resource.

```sh
app schema
```

### manifest

Outputs the resource manifest.

```sh
app manifest
```

## Requirements

- .NET Standard 2.0 or higher
- Depends on `OpenDsc.Resource` and `System.CommandLine`

## License

MIT
