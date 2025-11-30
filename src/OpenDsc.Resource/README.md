# OpenDsc.Resource

The `OpenDsc.Resource` package contains core types for Microsoft DSC
v3 resources.

## Installation

Install the package via NuGet:

```sh
dotnet add package OpenDsc.Resource
```

## Usage

This package provides the base classes and interfaces needed to implement
Microsoft DSC v3 resources in C#.

### Base Class

Inherit from `DscResource<T>` to get basic JSON serialization and schema
generation:

```csharp
using OpenDsc.Resource;
using System.Text.Json;

[DscResource("MyCompany/MyResource", "1.0.0")]
public class MyResource : DscResource<MySchema>
{
    public MyResource() : base(DscJsonSerializerSettings.Default) { }
}
```

### Interfaces

Implement the following interfaces to add DSC operations:

- `IGettable<T>`: Retrieve current configuration
- `ISettable<T>`: Set desired configuration
- `ITestable<T>`: Test if configuration matches desired state
- `IDeletable<T>`: Delete configuration
- `IExportable<T>`: Export all instances

### Attributes

Use the `DscResourceAttribute` to define resource metadata:

```csharp
[DscResource("MyCompany/MyResource", "1.0.0")]
public class MyResource : DscResource<MySchema>, IGettable<MySchema>, ISettable<MySchema>
{
    // Implementation
}
```

## Requirements

- .NET Standard 2.0 or higher
- Depends on `NuGet.Versioning` and `System.Text.Json`

## License

MIT
