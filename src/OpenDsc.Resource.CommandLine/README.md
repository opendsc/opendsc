# OpenDsc.Resource.CommandLine

The `OpenDsc.Resource.CommandLine` package contains a command-line and
resource manifest generator for Microsoft DSC v3 resources.

## Installation

Install the package via NuGet:

```sh
dotnet add package OpenDsc.Resource.CommandLine
```

## Usage

This package provides a `CommandBuilder` class that generates a
`System.CommandLine.RootCommand` for your DSC resource(s). The command
structure includes:

- `get`: Get the current state of a resource instance
- `set`: Set the desired state of a resource instance
- `test`: Test if a resource instance is in the desired state
- `delete`: Delete a resource instance
- `export`: Export all instances of a resource
- `schema`: Get the JSON schema for a resource
- `manifest`: Generate the DSC resource manifest(s)

### Single Resource Example

```csharp
using OpenDsc.Resource.CommandLine;

var resource = new Resource(SourceGenerationContext.Default);
var command = new CommandBuilder()
    .AddResource<Resource, Schema>(resource)
    .Build();
return command.Parse(args).Invoke();
```

### Multi-Resource Example

You can register multiple resources in a single executable:

```csharp
using OpenDsc.Resource.CommandLine;

var fileResource = new FileResource(SourceGenerationContext.Default);
var userResource = new UserResource(SourceGenerationContext.Default);
var serviceResource = new ServiceResource(SourceGenerationContext.Default);

var command = new CommandBuilder()
    .AddResource<FileResource, FileSchema>(fileResource)
    .AddResource<UserResource, UserSchema>(userResource)
    .AddResource<ServiceResource, ServiceSchema>(serviceResource)
    .Build();

return command.Parse(args).Invoke();
```

When multiple resources are registered, all commands require the `--resource`
parameter to specify which resource to operate on.

## Commands

### get

Retrieves the current state of the resource.

```sh
# Single resource
app get --input '{"property": "value"}'

# Multi-resource
app get --resource 'Owner/ResourceName' --input '{"property": "value"}'
```

### set

Sets the desired state of the resource.

```sh
# Single resource
app set --input '{"property": "value"}'

# Multi-resource
app set --resource 'Owner/ResourceName' --input '{"property": "value"}'
```

### test

Tests if the resource is in the desired state.

```sh
# Single resource
app test --input '{"property": "value"}'

# Multi-resource
app test --resource 'Owner/ResourceName' --input '{"property": "value"}'
```

### delete

Deletes the resource instance.

```sh
# Single resource
app delete --input '{"property": "value"}'

# Multi-resource
app delete --resource 'Owner/ResourceName' --input '{"property": "value"}'
```

### export

Exports all instances of the resource.

```sh
# Single resource
app export

# Multi-resource
app export --resource 'Owner/ResourceName'
```

### schema

Outputs the JSON schema for the resource.

```sh
# Single resource
app schema

# Multi-resource
app schema --resource 'Owner/ResourceName'
```

### manifest

Generates the resource manifest(s).

```sh
# Single resource - output to console
app manifest

# Single resource - save to file
app manifest --save

# Multi-resource - all manifests to console
app manifest

# Multi-resource - save all manifests to file
app manifest --save

# Multi-resource - specific resource manifest
app manifest --resource 'Owner/ResourceName'
```

## Requirements

- .NET Standard 2.0 or higher
- Depends on `OpenDsc.Resource` and `System.CommandLine`

## License

MIT
