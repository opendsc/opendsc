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
using System.Text.Json.Serialization;

[DscResource("MyCompany/MyResource", "1.0.0")]
public class MyResource(JsonSerializerContext context) : DscResource<MySchema>(context)
{
    // Constructor automatically passes context to base class
}
```

For non-AOT scenarios, you can use `JsonSerializerOptions`:

```csharp
public class MyResource(JsonSerializerOptions options) : DscResource<MySchema>(options)
{
    // Use DscJsonSerializerSettings.Default for standard settings
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
[DscResource("MyCompany/MyResource", "1.0.0",
    Description = "Manages my resource",
    Tags = ["tag1", "tag2"],
    SetReturn = SetReturn.StateAndDiff,
    TestReturn = TestReturn.StateAndDiff)]
public class MyResource(JsonSerializerContext context) : DscResource<MySchema>(context),
    IGettable<MySchema>, ISettable<MySchema>, ITestable<MySchema>
{
    public MySchema Get(MySchema instance) { /* implementation */ }
    public SetResult<MySchema>? Set(MySchema instance) { /* implementation */ }
    public TestResult<MySchema> Test(MySchema instance) { /* implementation */ }
}
```

The attribute supports:

- **Type** (required): Resource identifier in `Owner/Name` format
- **Version** (required): Semantic version string
- **Description**: Human-readable description
- **Tags**: Array of tags for categorization
- **SetReturn**: Controls `Set` operation output (None, State, StateAndDiff)
- **TestReturn**: Controls `Test` operation output (State, StateAndDiff)

## Requirements

- .NET Standard 2.0 or higher
- Depends on `NuGet.Versioning` and `System.Text.Json`

## License

MIT
