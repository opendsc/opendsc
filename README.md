# OpenDsc

A C# library for building Microsoft DSC v3 resources with ease.

## Features

- 🚀 Quick scaffolding with project templates
- 📦 Supports .NET Standard 2.0, .NET 8, and .NET 9
- ⚡ Native AOT compilation support
- 🔧 Automatic CLI generation
- 📋 Automatic JSON schema generation
- 📄 Automatic resource manifest generation
- 🎯 Type-safe DSC resource implementation

## Libraries

| Library | Description |
| --- | --- |
| [OpenDsc.Templates][t] | DSC project templates |
| [OpenDsc.Resource][r] | Core DSC resource implementation |
| [OpenDsc.Resource.CommandLine][c] | CLI and resource manifest generator |

[t]: https://www.nuget.org/packages/OpenDsc.Templates
[r]: https://www.nuget.org/packages/OpenDsc.Resource
[c]: https://www.nuget.org/packages/OpenDsc.Resource.CommandLine

## Quick Start

### 1. Install the Templates

```powershell
dotnet new install OpenDsc.Templates
```

### 2. Create a New DSC Resource Project

```powershell
dotnet new dsc --resource-name "MyCompany/MyResource" --resource-description "My DSC resource"
```

For Native AOT support:

```powershell
dotnet new dsc --aot true --resource-name "MyCompany/MyResource"
```

### 3. Implement Your Resource

```csharp
using OpenDsc.Resource;
using OpenDsc.Resource.CommandLine;

[DscResource("MyCompany/MyResource", Description = "Manage my resource")]
public class Resource : DscResource<Schema>, IGettable<Schema>
{
    public Resource(JsonSerializerContext context) : base(context) { }

    public Schema Get(Schema instance)
    {
        // Implementation
    }
}
```

### 4. Build and Run

```powershell
.\build.ps1
```

## Examples

See the [OpenDsc Resources repository](https://github.com/opendsc/resources)
for real-world examples:

- **Windows Management**: User accounts, groups, services, environment
  variables, optional features
- **File System**: Files, directories, access control lists
- **Cross-Platform**: XML element management, shortcuts

## Requirements

- .NET 8 SDK or later (for development)
- DSC v3 (for running resources)
- Windows, Linux, or macOS

## Documentation

For detailed documentation on each library, see their respective README files:

- [OpenDsc.Templates][td]
- [OpenDsc.Resource][rd]
- [OpenDsc.Resource.CommandLine][cd]

[td]: src/OpenDsc.Templates/README.md
[rd]: src/OpenDsc.Resource/README.md
[cd]: src/OpenDsc.Resource.CommandLine/README.md

## License

MIT
